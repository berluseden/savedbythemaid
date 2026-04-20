using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Application.DTOs.Booking;
using SavedByTheMaid.Application.Interfaces;
using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Errors;

namespace SavedByTheMaid.Application.Services;

/// <summary>
/// Application service for the public booking wizard flow.
/// Wraps the existing BookingService/SchedulingService with the Result pattern.
/// Handles coverage checks, estimates, availability, soft reserves, and booking confirmation.
/// </summary>
public class BookingApplicationService : IBookingApplicationService
{
    private readonly IApplicationDbContext _context;
    private readonly IBookingServiceAdapter _bookingService;
    private readonly ISchedulingServiceAdapter _schedulingService;
    private readonly ILogger<BookingApplicationService> _logger;

    public BookingApplicationService(
        IApplicationDbContext context,
        IBookingServiceAdapter bookingService,
        ISchedulingServiceAdapter schedulingService,
        ILogger<BookingApplicationService> logger)
    {
        _context = context;
        _bookingService = bookingService;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CoverageResponse>> CheckCoverageAsync(string zipCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                return Result<CoverageResponse>.Failure(
                    BookingErrors.ZipCodeNotCovered(zipCode ?? ""));
            }

            var serviceAreaZip = await _context.ServiceAreaZips
                .AsNoTracking()
                .Include(z => z.ServiceArea)
                .FirstOrDefaultAsync(z => z.ZipCode == zipCode && !z.IsDeleted);

            if (serviceAreaZip?.ServiceArea == null || !serviceAreaZip.ServiceArea.IsActive)
            {
                // Not covered is a valid business result, not a failure
                return Result<CoverageResponse>.Success(new CoverageResponse
                {
                    IsCovered = false,
                    Message = "Sorry, we don't cover this area yet."
                });
            }

            return Result<CoverageResponse>.Success(new CoverageResponse
            {
                IsCovered = true,
                ServiceAreaId = serviceAreaZip.ServiceAreaId,
                ServiceAreaName = serviceAreaZip.ServiceArea.Name,
                City = serviceAreaZip.City,
                State = serviceAreaZip.State,
                County = serviceAreaZip.County,
                Message = $"Great! We service {serviceAreaZip.City ?? "your area"}, {serviceAreaZip.State ?? ""}."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking coverage for ZIP code {ZipCode}", zipCode);
            return Result<CoverageResponse>.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result<EstimateResponse>> GetEstimateAsync(EstimateRequest request)
    {
        try
        {
            var pricing = await _bookingService.CalculatePricingAsync(new PricingInputDto
            {
                ServiceTypeId = request.ServiceTypeId,
                Rooms = request.Rooms?.Select(r => new RoomPricingItemDto(r.RoomId, r.Quantity)).ToList(),
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                AdditionalServiceIds = request.AdditionalServiceIds,
                SquareFootage = request.SquareFootage,
                DirtLevel = request.DirtLevel,
                HasPets = request.HasPets,
                HasElevator = request.HasElevator,
                IsFirstTime = request.IsFirstTime,
            });

            if (!pricing.Success)
            {
                return Result<EstimateResponse>.Failure(
                    BookingErrors.InvalidServiceType(request.ServiceTypeId));
            }

            return Result<EstimateResponse>.Success(new EstimateResponse
            {
                EstimatedMinutes = pricing.EstimatedMinutes,
                FormattedDuration = FormatDuration(pricing.EstimatedMinutes),
                Subtotal = pricing.Subtotal,
                Discount = pricing.Discount,
                Total = pricing.Total,
                DiscountPercent = pricing.DiscountPercent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating estimate for service type {ServiceTypeId}", request.ServiceTypeId);
            return Result<EstimateResponse>.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AvailabilityResponse>> GetAvailabilityAsync(AvailabilityRequest request)
    {
        try
        {
            // Validate service area
            var serviceAreaZip = await _context.ServiceAreaZips
                .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode);

            if (serviceAreaZip == null)
            {
                return Result<AvailabilityResponse>.Failure(
                    BookingErrors.ZipCodeNotCovered(request.ZipCode));
            }

            var serviceAreaId = serviceAreaZip.ServiceAreaId;
            var date = request.Date.Date;
            var dayOfWeek = date.DayOfWeek;

            // Get employees covering this area
            var employeesInZone = await _context.EmployeeServiceAreas
                .Where(e => e.ServiceAreaId == serviceAreaId && !e.IsDeleted)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            // Get active employees with schedule for that day
            var employees = await _context.Employees
                .Where(e => e.IsActive && !e.IsDeleted)
                .Where(e => employeesInZone.Contains(e.Id))
                .Include(e => e.Schedules.Where(s => s.DayOfWeek == dayOfWeek && s.IsAvailable))
                .Include(e => e.TimeOffs.Where(t =>
                    t.Status == TimeOffStatus.Approved &&
                    t.StartDateTime <= date.AddDays(1) &&
                    t.EndDateTime >= date))
                .ToListAsync();

            // Filter by required equipment if specified
            if (request.RequiredEquipmentIds?.Any() == true)
            {
                var employeesWithEquipment = await _context.EmployeeEquipment
                    .Where(e => request.RequiredEquipmentIds.Contains(e.EquipmentId) && e.IsAvailable)
                    .Select(e => e.EmployeeId)
                    .Distinct()
                    .ToListAsync();

                employees = employees.Where(e => employeesWithEquipment.Contains(e.Id)).ToList();
            }

            var slots = new List<TimeSlotDto>();

            foreach (var employee in employees)
            {
                var schedule = employee.Schedules.FirstOrDefault();
                if (schedule == null) continue;

                // Check if employee has time-off that day
                var hasTimeOff = employee.TimeOffs.Any(t =>
                    t.IsAllDay ||
                    (t.StartDateTime.Date <= date && t.EndDateTime.Date >= date));

                if (hasTimeOff) continue;

                // Get existing meetings for that day
                var existingMeetings = await _context.ServiceMeets
                    .Where(m => m.AssignedEmployeeId == employee.Id)
                    .Where(m => m.ScheduledStart.Date == date)
                    .Where(m => m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow)
                    .Select(m => new { m.ScheduledStart, m.ScheduledEnd })
                    .ToListAsync();

                // Get active soft reserves
                var activeSoftReserves = await _context.SoftReserves
                    .Where(s => s.EmployeeId == employee.Id)
                    .Where(s => s.ScheduledStart.Date == date)
                    .Where(s => s.Status == SoftReserveStatus.Active && s.ExpiresAt > DateTime.UtcNow)
                    .Select(s => new { s.ScheduledStart, s.ScheduledEnd })
                    .ToListAsync();

                // Combine occupancy
                var occupied = existingMeetings
                    .Concat(activeSoftReserves)
                    .Select(o => (Start: o.ScheduledStart, End: o.ScheduledEnd))
                    .ToList();

                // Generate slots every 30 minutes (accounting for buffer between services)
                var bufferMinutes = schedule.BufferMinutes;
                var slotStart = date.Add(schedule.StartTime);
                var dayEnd = date.Add(schedule.EndTime);
                var duration = TimeSpan.FromMinutes(request.EstimatedMinutes);

                while (slotStart.Add(duration) <= dayEnd)
                {
                    var slotEnd = slotStart.Add(duration);

                    // Check for overlap (with buffer)
                    var hasConflict = occupied.Any(o =>
                        slotStart < o.End.AddMinutes(bufferMinutes) && slotEnd.AddMinutes(bufferMinutes) > o.Start);

                    if (!hasConflict)
                    {
                        // Check if an identical slot already exists
                        var existingSlot = slots.FirstOrDefault(s =>
                            s.StartTime == slotStart.TimeOfDay);

                        if (existingSlot != null)
                        {
                            existingSlot.AvailableEmployeeIds.Add(employee.Id);
                        }
                        else
                        {
                            slots.Add(new TimeSlotDto
                            {
                                Date = date,
                                StartTime = slotStart.TimeOfDay,
                                EndTime = slotEnd.TimeOfDay,
                                FormattedTime = $"{slotStart:hh:mm tt} - {slotEnd:hh:mm tt}",
                                AvailableEmployeeIds = new List<int> { employee.Id }
                            });
                        }
                    }

                    slotStart = slotStart.AddMinutes(30);
                }
            }

            return Result<AvailabilityResponse>.Success(new AvailabilityResponse
            {
                Date = date,
                ZipCode = request.ZipCode,
                ServiceAreaId = serviceAreaId,
                Slots = slots.OrderBy(s => s.StartTime).ToList(),
                TotalSlotsAvailable = slots.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting availability for ZIP {ZipCode} on {Date}", request.ZipCode, request.Date);
            return Result<AvailabilityResponse>.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoftReserveResponse>> CreateSoftReserveAsync(CreateSoftReserveRequest request)
    {
        try
        {
            // Get ServiceAreaId from ZipCode
            var serviceAreaZip = await _context.ServiceAreaZips
                .Include(z => z.ServiceArea)
                .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode && z.ServiceArea!.IsActive);

            if (serviceAreaZip == null)
            {
                return Result<SoftReserveResponse>.Failure(
                    BookingErrors.ZipCodeNotCovered(request.ZipCode));
            }

            var serviceAreaId = serviceAreaZip.ServiceAreaId;
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var ttlMinutes = 15;
            var expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);

            var startDateTime = request.Date.Date.Add(request.StartTime);
            var endDateTime = startDateTime.AddMinutes(request.EstimatedMinutes);

            // Verify employee has a work schedule for that day
            var dayOfWeek = request.Date.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(s =>
                    s.EmployeeId == request.EmployeeId &&
                    s.DayOfWeek == dayOfWeek &&
                    s.IsAvailable);

            if (schedule == null)
            {
                return Result<SoftReserveResponse>.Failure(
                    EmployeeErrors.NotAvailableOnDay(request.EmployeeId.GetValueOrDefault(), dayOfWeek));
            }

            // Verify the time is within the work shift
            if (request.StartTime < schedule.StartTime || request.StartTime >= schedule.EndTime)
            {
                return Result<SoftReserveResponse>.Failure(
                    EmployeeErrors.OutsideWorkingHours(request.EmployeeId.GetValueOrDefault()));
            }

            // Check conflicts using scheduling service
            var conflict = await _schedulingService.CheckConflictsAsync(
                request.EmployeeId.GetValueOrDefault(), startDateTime, endDateTime);

            if (conflict != null)
            {
                _logger.LogWarning("Conflict detected creating SoftReserve: {Message}", conflict.Message);
                return Result<SoftReserveResponse>.Failure(BookingErrors.SlotUnavailable);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create soft reserve
                var softReserve = new SoftReserve
                {
                    SessionId = sessionId,
                    CustomerId = request.CustomerId,
                    EmployeeId = request.EmployeeId,
                    ServiceAreaId = serviceAreaId,
                    ScheduledStart = startDateTime,
                    ScheduledEnd = endDateTime,
                    ExpiresAt = expiresAt,
                    Status = SoftReserveStatus.Active
                };

                _context.SoftReserves.Add(softReserve);
                await _context.SaveChangesAsync();

                // Acquire slots for anti-collision
                await _schedulingService.AcquireSlotsAsync(
                    request.EmployeeId,
                    startDateTime,
                    endDateTime,
                    OccupancyType.SoftReserve,
                    softReserve.Id,
                    expiresAt);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "SoftReserve {SoftReserveId} created for employee {EmployeeId}",
                    softReserve.Id, request.EmployeeId);

                return Result<SoftReserveResponse>.Success(new SoftReserveResponse
                {
                    SoftReserveId = softReserve.Id,
                    SessionId = sessionId,
                    ScheduledStart = startDateTime,
                    ScheduledEnd = endDateTime,
                    ExpiresAt = softReserve.ExpiresAt,
                    TtlSeconds = ttlMinutes * 60,
                    Message = $"Time slot reserved for {ttlMinutes} minutes. Complete payment to confirm."
                });
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Constraint violation = slot already taken by another user
                _logger.LogWarning(
                    "Slot conflict for employee {EmployeeId} at {Start} - {End}",
                    request.EmployeeId, startDateTime, endDateTime);

                return Result<SoftReserveResponse>.Failure(BookingErrors.SlotUnavailable);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating soft reserve");
            return Result<SoftReserveResponse>.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result<BookingConfirmationResponse>> ConfirmBookingAsync(ConfirmBookingRequest request)
    {
        try
        {
            var result = await _bookingService.ConfirmBookingAsync(new ConfirmBookingInputDto
            {
                SoftReserveId = request.SoftReserveId,
                SessionId = request.SessionId,
                CustomerId = request.CustomerId,
                ZipCode = request.ZipCode,
                Address = request.Address,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                ServiceTypeId = request.ServiceTypeId,
                CleaningPlaceId = request.CleaningPlaceId,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFootage = request.SquareFootage,
                DirtLevel = request.DirtLevel,
                HasPets = request.HasPets,
                FloorLevel = request.FloorLevel,
                HasElevator = request.HasElevator,
                AdditionalServiceIds = request.AdditionalServiceIds,
                Rooms = request.Rooms?.Select(r => new RoomPricingItemDto(r.RoomId, r.Quantity)).ToList(),
                Total = request.Total,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Password = request.Password,
                SpecialInstructions = request.SpecialInstructions
            });

            if (!result.Success)
            {
                if (result.IsNotFound)
                    return Result<BookingConfirmationResponse>.Failure(BookingErrors.SoftReserveNotFound(request.SoftReserveId));

                if (result.IsExpired)
                    return Result<BookingConfirmationResponse>.Failure(BookingErrors.SoftReserveExpired);

                if (result.IsAlreadyProcessed)
                    return Result<BookingConfirmationResponse>.Failure(BookingErrors.InvalidSoftReserve);

                return Result<BookingConfirmationResponse>.Failure(
                    new Error("Booking.ConfirmFailed", result.Error ?? "Failed to confirm booking."));
            }

            return Result<BookingConfirmationResponse>.Success(new BookingConfirmationResponse
            {
                OrderId = result.OrderId,
                MeetId = result.MeetId,
                ConfirmationNumber = result.ConfirmationNumber,
                ScheduledStart = result.ScheduledStart,
                ScheduledEnd = result.ScheduledEnd,
                Total = result.Total,
                OrderStatus = result.OrderStatus,
                Message = result.Message,
                AuthToken = result.AuthToken != null ? new AuthTokenDto
                {
                    ExpiresAt = result.AuthToken.ExpiresAt,
                    IsNewUser = result.AuthToken.IsNewUser
                } : null,
                IsGuest = result.IsGuest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking for soft reserve {SoftReserveId}", request.SoftReserveId);
            return Result<BookingConfirmationResponse>.Failure(Error.Unexpected);
        }
    }

    /// <summary>
    /// Formats a duration in minutes to a human-readable string.
    /// </summary>
    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0
            ? $"{hours}h {mins}min"
            : $"{mins} min";
    }

    /// <summary>
    /// Detects if a database exception is a unique constraint violation.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";
        return innerMessage.Contains("unique") ||
               innerMessage.Contains("duplicate") ||
               innerMessage.Contains("23505") ||
               innerMessage.Contains("1062") ||
               innerMessage.Contains("2601") ||
               innerMessage.Contains("2627");
    }
}
