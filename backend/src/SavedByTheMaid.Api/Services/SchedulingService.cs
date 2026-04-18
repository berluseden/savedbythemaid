using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

/// <summary>
/// Scheduling service that handles conflict validation and SlotOccupancy management.
/// Implements the anti-collision model using the SlotOccupancy table with UNIQUE(EmployeeId, SlotStart).
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Checks whether a conflict exists for an employee in a given time range.
    /// </summary>
    /// <param name="employeeId">ID of the employee to check</param>
    /// <param name="start">Start of the time range</param>
    /// <param name="end">End of the time range</param>
    /// <param name="excludeMeetingId">ID of the meeting to exclude (for rescheduling the same meeting)</param>
    /// <returns>null if there is no conflict, SchedulingConflict with details if one exists</returns>
    Task<SchedulingConflict?> CheckConflictsAsync(int employeeId, DateTime start, DateTime end, int? excludeMeetingId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires slots in SlotOccupancy for an employee in a given time range.
    /// Slots are created with 30-minute granularity.
    /// </summary>
    /// <param name="employeeId">ID of the employee</param>
    /// <param name="start">Start of the range</param>
    /// <param name="end">End of the range</param>
    /// <param name="type">Occupancy type (SoftReserve or Meeting)</param>
    /// <param name="referenceId">Reference ID (SoftReserve or ServiceMeet)</param>
    /// <param name="expiresAt">Expiration date (only for SoftReserve)</param>
    Task AcquireSlotsAsync(int employeeId, DateTime start, DateTime end, OccupancyType type, int referenceId, DateTime? expiresAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases SlotOccupancy slots by referenceId and type.
    /// </summary>
    /// <param name="referenceId">Reference ID</param>
    /// <param name="type">Occupancy type</param>
    Task ReleaseSlotsAsync(int referenceId, OccupancyType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers slots from one employee to another (for reassignment).
    /// Verifies the new employee has no conflicts before transferring.
    /// </summary>
    /// <param name="referenceId">Meeting reference ID</param>
    /// <param name="type">Occupancy type</param>
    /// <param name="newEmployeeId">New employee to transfer to</param>
    /// <returns>Conflict if transfer would cause double-booking, null on success</returns>
    Task<SchedulingConflict?> TransferSlotsAsync(int referenceId, OccupancyType type, int newEmployeeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Conflict verification result with details.
/// </summary>
public record SchedulingConflict
{
    /// <summary>
    /// Detected conflict type
    /// </summary>
    public ConflictType Type { get; init; }

    /// <summary>
    /// Descriptive conflict message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// ID of the conflicting entity (Meeting, TimeOff, etc.)
    /// </summary>
    public int? ConflictingEntityId { get; init; }

    /// <summary>
    /// Start of the conflicting range
    /// </summary>
    public DateTime? ConflictStart { get; init; }

    /// <summary>
    /// End of the conflicting range
    /// </summary>
    public DateTime? ConflictEnd { get; init; }

    /// <summary>
    /// ID of the affected employee
    /// </summary>
    public int EmployeeId { get; init; }

    /// <summary>
    /// Employee name (if available)
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Additional conflict details
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Possible conflict types
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Conflict with another existing meeting/reservation
    /// </summary>
    ExistingBooking = 0,

    /// <summary>
    /// Conflict with the employee's approved time off
    /// </summary>
    TimeOff = 1,

    /// <summary>
    /// Employee not available in the service area
    /// </summary>
    AreaMismatch = 2,

    /// <summary>
    /// Inactive or invalid employee
    /// </summary>
    EmployeeUnavailable = 3
}

public class SchedulingService : ISchedulingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulingService> _logger;
    private const int SlotGranularityMinutes = 30;

    public SchedulingService(ApplicationDbContext context, ILogger<SchedulingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SchedulingConflict?> CheckConflictsAsync(int employeeId, DateTime start, DateTime end, int? excludeMeetingId = null, CancellationToken cancellationToken = default)
    {
        // Normalize DateTimeKind — frontend may omit the Z suffix, producing Unspecified
        start = NormalizeUtc(start);
        end = NormalizeUtc(end);

        _logger.LogInformation(
            "Checking conflicts for employee {EmployeeId} in range {Start} - {End}, excluding meeting {ExcludeMeetingId}",
            employeeId, start, end, excludeMeetingId);

        // Verify that the employee exists and is active
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
        {
            return new SchedulingConflict
            {
                Type = ConflictType.EmployeeUnavailable,
                Message = "The specified employee does not exist",
                EmployeeId = employeeId
            };
        }

        if (!employee.IsActive)
        {
            return new SchedulingConflict
            {
                Type = ConflictType.EmployeeUnavailable,
                Message = $"Employee {employee.FirstName} {employee.LastName} is inactive",
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}"
            };
        }

        // 0. Expand range by BufferMinutes from employee schedule for that day
        var dayOfWeek = start.DayOfWeek;
        var schedule = await _context.EmployeeSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.DayOfWeek == dayOfWeek && s.IsAvailable && !s.IsDeleted, cancellationToken);

        var bufferMinutes = schedule?.BufferMinutes ?? 0;
        var bufferedStart = start.AddMinutes(-bufferMinutes);
        var bufferedEnd = end.AddMinutes(bufferMinutes);

        // 1. Check conflicts in SlotOccupancy (using buffered range)
        var slotConflictQuery = _context.SlotOccupancies
            .Where(s => s.EmployeeId == employeeId && !s.IsDeleted)
            .Where(s => s.SlotStart < bufferedEnd && s.SlotEnd > bufferedStart);

        // Exclude the current meeting if this is a reschedule
        if (excludeMeetingId.HasValue)
        {
            slotConflictQuery = slotConflictQuery.Where(s => 
                !(s.OccupancyType == OccupancyType.Meeting && s.ReferenceId == excludeMeetingId.Value));
        }

        var existingSlot = await slotConflictQuery
            .OrderBy(s => s.SlotStart)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSlot != null)
        {
            // Get details of the conflicting meeting or reservation
            string details = existingSlot.OccupancyType == OccupancyType.Meeting
                ? await GetMeetingDetailsAsync(existingSlot.ReferenceId, cancellationToken)
                : $"Temporary reservation (expires: {existingSlot.ExpiresAt:HH:mm})";

            return new SchedulingConflict
            {
                Type = ConflictType.ExistingBooking,
                Message = $"The employee already has a {(existingSlot.OccupancyType == OccupancyType.Meeting ? "meeting" : "reservation")} at that time",
                ConflictingEntityId = existingSlot.ReferenceId,
                ConflictStart = existingSlot.SlotStart,
                ConflictEnd = existingSlot.SlotEnd,
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Details = details
            };
        }

        // 2. Check conflicts with approved EmployeeTimeOff
        var timeOffConflict = await _context.EmployeeTimeOffs
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && !t.IsDeleted)
            .Where(t => t.Status == TimeOffStatus.Approved)
            .Where(t => t.StartDateTime < bufferedEnd && t.EndDateTime > bufferedStart)
            .FirstOrDefaultAsync(cancellationToken);

        if (timeOffConflict != null)
        {
            string timeOffType = timeOffConflict.Type switch
            {
                TimeOffType.Vacation => "vacation",
                TimeOffType.Sick => "sick leave",
                TimeOffType.Personal => "personal leave",
                TimeOffType.ManualBlock => "manual block",
                _ => "time off"
            };

            return new SchedulingConflict
            {
                Type = ConflictType.TimeOff,
                Message = $"The employee has approved {timeOffType} at that time",
                ConflictingEntityId = timeOffConflict.Id,
                ConflictStart = timeOffConflict.StartDateTime,
                ConflictEnd = timeOffConflict.EndDateTime,
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Details = timeOffConflict.Reason
            };
        }

        // 3. Enforce MaxDailyHours and MaxDailyServices limits
        var serviceDate = start.Date;
        var nextDate = serviceDate.AddDays(1);
        var durationMinutes = (end - start).TotalMinutes;

        // Count existing meetings for that day (excluding cancelled/noshow)
        var dailyMeetings = await _context.ServiceMeets
            .AsNoTracking()
            .Where(m => m.AssignedEmployeeId == employeeId && !m.IsDeleted)
            .Where(m => m.ScheduledStart >= serviceDate && m.ScheduledStart < nextDate)
            .Where(m => m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow)
            .Where(m => excludeMeetingId == null || m.Id != excludeMeetingId)
            .ToListAsync(cancellationToken);

        if (employee.MaxDailyServices > 0 && dailyMeetings.Count >= employee.MaxDailyServices)
        {
            return new SchedulingConflict
            {
                Type = ConflictType.EmployeeUnavailable,
                Message = $"The employee has reached the maximum of {employee.MaxDailyServices} daily services",
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}"
            };
        }

        if (employee.MaxDailyHours > 0)
        {
            var existingMinutes = dailyMeetings.Sum(m => (m.ScheduledEnd - m.ScheduledStart).TotalMinutes);
            if (existingMinutes + durationMinutes > employee.MaxDailyHours * 60)
            {
                return new SchedulingConflict
                {
                    Type = ConflictType.EmployeeUnavailable,
                    Message = $"The employee would exceed the maximum of {employee.MaxDailyHours} daily hours",
                    EmployeeId = employeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}"
                };
            }
        }

        _logger.LogInformation("No conflicts found for employee {EmployeeId}", employeeId);
        return null; // No conflicts
    }

    public async Task AcquireSlotsAsync(int employeeId, DateTime start, DateTime end, OccupancyType type, int referenceId, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        // Normalize DateTimeKind — frontend may omit the Z suffix, producing Unspecified
        start = NormalizeUtc(start);
        end = NormalizeUtc(end);

        _logger.LogInformation(
            "Acquiring slots for employee {EmployeeId}, range {Start} - {End}, type {Type}, ref {ReferenceId}",
            employeeId, start, end, type, referenceId);

        var slots = CalculateSlots(start, end);

        var slotOccupancies = slots.Select(slot => new SlotOccupancy
        {
            EmployeeId = employeeId,
            SlotStart = slot.Start,
            SlotEnd = slot.End,
            OccupancyType = type,
            ReferenceId = referenceId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.SlotOccupancies.AddRange(slotOccupancies);
        
        // We do not call SaveChanges here - it should be called within an external transaction
        _logger.LogInformation("Prepared {Count} slots for insertion", slotOccupancies.Count);
    }

    public async Task ReleaseSlotsAsync(int referenceId, OccupancyType type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Releasing slots for reference {ReferenceId}, type {Type}",referenceId, type);

        var slotsToRemove = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == referenceId && s.OccupancyType == type && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        if (slotsToRemove.Any())
        {
            _context.SlotOccupancies.RemoveRange(slotsToRemove);
            _logger.LogInformation("Marked {Count} slots for deletion", slotsToRemove.Count);
        }
        else
        {
            _logger.LogWarning("No slots found to release for reference {ReferenceId}", referenceId);
        }
    }

    public async Task<SchedulingConflict?> TransferSlotsAsync(int referenceId, OccupancyType type, int newEmployeeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Transferring slots for reference {ReferenceId} to employee {NewEmployeeId}",
            referenceId, newEmployeeId);

        var slotsToTransfer = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == referenceId && s.OccupancyType == type && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!slotsToTransfer.Any())
        {
            _logger.LogWarning("No slots found to transfer for reference {ReferenceId}", referenceId);
            return null;
        }

        // Verify new employee has no conflicts in the slot time range
        var earliest = slotsToTransfer.Min(s => s.SlotStart);
        var latest = slotsToTransfer.Max(s => s.SlotEnd);
        var conflict = await CheckConflictsAsync(newEmployeeId, earliest, latest, cancellationToken: cancellationToken);
        if (conflict != null)
        {
            _logger.LogWarning("Transfer blocked: conflict for employee {EmployeeId} in {Start}-{End}",
                newEmployeeId, earliest, latest);
            return conflict;
        }

        foreach (var slot in slotsToTransfer)
        {
            slot.EmployeeId = newEmployeeId;
            slot.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Transferred {Count} slots to employee {NewEmployeeId}",
            slotsToTransfer.Count, newEmployeeId);
        return null;
    }

    /// <summary>
    /// Normalizes a DateTime to UTC regardless of its Kind.
    /// Unspecified datetimes (e.g. from frontend requests missing the Z suffix) are assumed to be UTC.
    /// </summary>
    private static DateTime NormalizeUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc) // Unspecified — treat as UTC
    };

    /// <summary>
    /// Calculates the 30-minute slots for a time range.
    /// </summary>
    private static List<(DateTime Start, DateTime End)> CalculateSlots(DateTime start, DateTime end)
    {
        // Normalize DateTimeKind before computing slots
        start = NormalizeUtc(start);
        end = NormalizeUtc(end);

        var slots = new List<(DateTime Start, DateTime End)>();
        var currentSlot = NormalizeToSlotBoundary(start);

        while (currentSlot < end)
        {
            var slotEnd = currentSlot.AddMinutes(SlotGranularityMinutes);
            slots.Add((currentSlot, slotEnd));
            currentSlot = slotEnd;
        }

        return slots;
    }

    /// <summary>
    /// Normalizes a date/time down to the nearest slot boundary (30-min granularity).
    /// Input is normalized to UTC first to guard against Unspecified kind.
    /// </summary>
    private static DateTime NormalizeToSlotBoundary(DateTime dateTime)
    {
        dateTime = NormalizeUtc(dateTime);
        var normalizedMinutes = (dateTime.Minute / SlotGranularityMinutes) * SlotGranularityMinutes;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, normalizedMinutes, 0, dateTime.Kind);
    }

    private async Task<string> GetMeetingDetailsAsync(int meetingId, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.ServiceMeets
            .AsNoTracking()
            .Include(m => m.ServiceOrder)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

        if (meeting == null)
            return "Meeting not found";

        return $"Meeting #{meeting.Id} ({meeting.ScheduledStart:HH:mm} - {meeting.ScheduledEnd:HH:mm}) - {meeting.ServiceOrder?.Address ?? "No address"}";
    }
}
