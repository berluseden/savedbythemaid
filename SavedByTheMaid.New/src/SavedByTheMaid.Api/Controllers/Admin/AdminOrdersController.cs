using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminOrdersController> _logger;
    private readonly ISchedulingService _schedulingService;
    private readonly IStatusHistoryService _statusHistoryService;

    public AdminOrdersController(
        ApplicationDbContext context,
        ILogger<AdminOrdersController> logger,
        ISchedulingService schedulingService,
        IStatusHistoryService statusHistoryService)
    {
        _context = context;
        _logger = logger;
        _schedulingService = schedulingService;
        _statusHistoryService = statusHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetAll(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? serviceAreaId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Querying orders - Status: {Status}, From: {From}, To: {To}, Page: {Page}",
            status, from, to, page);
            
        var query = _context.ServiceOrders
            .Where(o => !o.IsDeleted);

        if (status.HasValue)
            query = query.Where(o => o.OrderStatus == status.Value);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        if (serviceAreaId.HasValue)
            query = query.Where(o => o.ServiceAreaId == serviceAreaId.Value);

        return await query
            .Include(o => o.ServiceArea)
            .Include(o => o.ServiceType)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                ConfirmationNumber = $"SBM-{o.Id:D6}",
                ContactName = o.ContactName,
                ContactPhone = o.ContactPhone,
                Address = o.Address,
                City = o.City,
                ZipCode = o.ZipCode,
                ServiceAreaName = o.ServiceArea != null ? o.ServiceArea.Name : null,
                ServiceTypeName = o.ServiceType != null ? o.ServiceType.Name : null,
                Total = o.Total,
                OrderStatus = o.OrderStatus,
                RecurrenceType = o.RecurrenceType,
                CreatedAt = o.CreatedAt,
                ScheduledDate = o.Meetings.OrderBy(m => m.ScheduledStart).Select(m => (DateTime?)m.ScheduledStart).FirstOrDefault()
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceOrder>> GetById(int id)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.ServiceArea)
            .Include(o => o.ServiceType)
            .Include(o => o.CleaningPlace)
            .Include(o => o.Items).ThenInclude(i => i.AdditionalServiceType)
            .Include(o => o.Rooms).ThenInclude(r => r.CleaningPlaceRoom)
            .Include(o => o.Meetings).ThenInclude(m => m.AssignedEmployee)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        return order == null ? NotFound() : order;
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null || order.IsDeleted) return NotFound();

        // Validate allowed OrderStatus transitions
        var currentStatus = order.OrderStatus;
        var newStatus = request.OrderStatus;

        // Define valid transitions (includes PendingReview)
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.PendingReview] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            #pragma warning disable CS0618
            [OrderStatus.Draft] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            #pragma warning restore CS0618
            [OrderStatus.Confirmed] = new[] { OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow },
            [OrderStatus.InProgress] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
            [OrderStatus.Completed] = Array.Empty<OrderStatus>(), // Final state
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(), // Final state
            [OrderStatus.NoShow] = Array.Empty<OrderStatus>() // Final state
        };

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            return BadRequest($"Invalid transition: {currentStatus} -> {newStatus}");
        }

        _logger.LogInformation("Order {OrderId} status transition: {From} -> {To}", 
            id, currentStatus, newStatus);

        order.OrderStatus = newStatus;
        await _context.SaveChangesAsync();

        // Record audit trail
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _statusHistoryService.RecordOrderStatusChangeAsync(
            id, currentStatus, newStatus, userId, notes: $"Admin status change: {currentStatus} -> {newStatus}");

        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest? request = null)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Meetings)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || order.IsDeleted) return NotFound();

        order.OrderStatus = OrderStatus.Cancelled;

        // Cancel all pending appointments
        foreach (var meet in order.Meetings.Where(m => 
            m.Status == MeetStatus.Scheduled || 
            m.Status == MeetStatus.Assigned ||
            m.Status == MeetStatus.Rescheduled))
        {
            meet.Status = MeetStatus.Cancelled;
            meet.CancellationReason = request?.Reason ?? "Order cancelled";
            
            // Release occupied slots if an employee is assigned
            if (meet.AssignedEmployeeId.HasValue)
            {
                await _schedulingService.ReleaseSlotsAsync(meet.Id, OccupancyType.Meeting);
            }
        }

        await _context.SaveChangesAsync();

        // Record audit trail for cancellation
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _statusHistoryService.RecordOrderStatusChangeAsync(
            id, order.OrderStatus, OrderStatus.Cancelled, adminId,
            reasonCode: "ADMIN_CANCEL", notes: request?.Reason ?? "Order cancelled by admin");

        return NoContent();
    }

    // Meetings (Appointments)
    [HttpGet("meetings")]
    public async Task<ActionResult<IEnumerable<MeetingSummaryDto>>> GetMeetings(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? serviceAreaId = null,
        [FromQuery] MeetStatus? status = null,
        [FromQuery] int? orderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.ServiceMeets
            .Where(m => !m.IsDeleted);

        if (date.HasValue)
            query = query.Where(m => m.ScheduledStart.Date == date.Value.Date);

        if (employeeId.HasValue)
            query = query.Where(m => m.AssignedEmployeeId == employeeId.Value);

        if (serviceAreaId.HasValue)
            query = query.Where(m => m.ServiceAreaId == serviceAreaId.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (orderId.HasValue)
            query = query.Where(m => m.ServiceOrderId == orderId.Value);

        return await query
            .Include(m => m.ServiceOrder)
            .Include(m => m.AssignedEmployee)
            .Include(m => m.ServiceArea)
            .OrderBy(m => m.ScheduledStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MeetingSummaryDto
            {
                Id = m.Id,
                OrderId = m.ServiceOrderId,
                ConfirmationNumber = $"SBM-{m.ServiceOrderId:D6}",
                ScheduledStart = m.ScheduledStart,
                ScheduledEnd = m.ScheduledEnd,
                ActualStart = m.ActualStart,
                ActualEnd = m.ActualEnd,
                EmployeeId = m.AssignedEmployeeId,
                EmployeeName = m.AssignedEmployee != null 
                    ? $"{m.AssignedEmployee.FirstName} {m.AssignedEmployee.LastName}" 
                    : null,
                ServiceAreaName = m.ServiceArea != null ? m.ServiceArea.Name : null,
                Address = m.ServiceOrder != null ? m.ServiceOrder.Address : null,
                ContactName = m.ServiceOrder != null ? m.ServiceOrder.ContactName : null,
                ContactPhone = m.ServiceOrder != null ? m.ServiceOrder.ContactPhone : null,
                Status = m.Status,
                EstimatedDurationMinutes = m.EstimatedDurationMinutes,
                AdjustmentStatus = m.AdjustmentStatus,
                AdjustmentAmount = m.AdjustmentAmount
            })
            .ToListAsync();
    }

    [HttpGet("meetings/{id}")]
    public async Task<ActionResult<ServiceMeet>> GetMeetingById(int id)
    {
        var meet = await _context.ServiceMeets
            .Include(m => m.ServiceOrder)
                .ThenInclude(o => o!.Items)
            .Include(m => m.AssignedEmployee)
            .Include(m => m.ServiceArea)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        return meet == null ? NotFound() : meet;
    }

    [HttpPut("meetings/{id}/status")]
    public async Task<IActionResult> UpdateMeetingStatus(int id, UpdateMeetingStatusRequest request)
    {
        var meet = await _context.ServiceMeets.FindAsync(id);
        if (meet == null || meet.IsDeleted) return NotFound();

        var previousStatus = meet.Status;
        meet.Status = request.Status;

        if (request.Status == MeetStatus.InProgress && !meet.ActualStart.HasValue)
            meet.ActualStart = DateTime.UtcNow;

        if (request.Status == MeetStatus.Completed && !meet.ActualEnd.HasValue)
            meet.ActualEnd = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
            meet.Notes = request.Notes;

        await _context.SaveChangesAsync();

        // Record audit trail
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _statusHistoryService.RecordMeetStatusChangeAsync(
            id, previousStatus, request.Status, adminId, notes: request.Notes);

        return NoContent();
    }

    [HttpPut("meetings/{id}/assign")]
    public async Task<IActionResult> AssignEmployee(int id, AssignEmployeeRequest request)
    {
        var meet = await _context.ServiceMeets.FindAsync(id);
        if (meet == null || meet.IsDeleted) return NotFound();

        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null || !employee.IsActive)
            return BadRequest("Invalid or inactive employee");

        // Verify employee works on that day/time
        var meetDayOfWeek = meet.ScheduledStart.DayOfWeek;
        var meetStartTime = meet.ScheduledStart.TimeOfDay;
        var meetEndTime = meet.ScheduledEnd.TimeOfDay;
        var empSchedule = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == request.EmployeeId &&
                s.DayOfWeek == meetDayOfWeek &&
                s.IsAvailable && !s.IsDeleted);

        if (empSchedule == null)
            return BadRequest($"The employee does not work on {meetDayOfWeek}");

        if (meetStartTime < empSchedule.StartTime || meetEndTime > empSchedule.EndTime)
            return BadRequest("The appointment is outside the employee's working hours");

        // Check for conflicts for the new employee
        var conflict = await _schedulingService.CheckConflictsAsync(
            request.EmployeeId,
            meet.ScheduledStart,
            meet.ScheduledEnd,
            excludeMeetingId: null // We don't exclude anything because it's a different employee
        );

        if (conflict != null)
        {
            _logger.LogWarning(
                "Conflict when assigning employee {EmployeeId} to meeting {MeetingId}: {ConflictType}",
                request.EmployeeId, id, conflict.Type);

            return Conflict(new SchedulingConflictResponse
            {
                Error = "SCHEDULING_CONFLICT",
                Message = conflict.Message,
                Conflict = conflict
            });
        }

        // Use transaction for consistency
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // If an employee was already assigned, transfer the slots
            if (meet.AssignedEmployeeId.HasValue && meet.AssignedEmployeeId.Value != request.EmployeeId)
            {
                var transferConflict = await _schedulingService.TransferSlotsAsync(
                    meet.Id,
                    OccupancyType.Meeting,
                    request.EmployeeId);
                if (transferConflict != null)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new SchedulingConflictResponse
                    {
                        Error = "SCHEDULING_CONFLICT",
                        Message = transferConflict.Message,
                        Conflict = transferConflict
                    });
                }
            }
            else if (!meet.AssignedEmployeeId.HasValue)
            {
                // First assignment - create new slots
                await _schedulingService.AcquireSlotsAsync(
                    request.EmployeeId,
                    meet.ScheduledStart,
                    meet.ScheduledEnd,
                    OccupancyType.Meeting,
                    meet.Id);
            }

            meet.AssignedEmployeeId = request.EmployeeId;
            meet.Status = MeetStatus.Assigned;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Employee {EmployeeId} successfully assigned to meeting {MeetingId}",
                request.EmployeeId, id);

            return NoContent();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex,
                "Unique constraint violation when assigning employee {EmployeeId} to meeting {MeetingId}",
                request.EmployeeId, id);

            return Conflict(new SchedulingConflictResponse
            {
                Error = "SLOT_ALREADY_TAKEN",
                Message = "The time slot was reserved by a concurrent operation. Please try again.",
                Conflict = new SchedulingConflict
                {
                    Type = ConflictType.ExistingBooking,
                    Message = "Concurrency conflict detected",
                    EmployeeId = request.EmployeeId
                }
            });
        }
    }

    [HttpPost("meetings/{id}/reschedule")]
    public async Task<IActionResult> RescheduleMeeting(int id, RescheduleMeetingRequest request)
    {
        var meet = await _context.ServiceMeets
            .Include(m => m.AssignedEmployee)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (meet == null) return NotFound();

        var newEnd = request.NewStart.AddMinutes(meet.EstimatedDurationMinutes);

        // Determine the employee for conflict checking
        var employeeIdToCheck = request.NewEmployeeId ?? meet.AssignedEmployeeId;
        
        if (!employeeIdToCheck.HasValue)
        {
            return BadRequest("The meeting has no assigned employee and no new one was provided");
        }

        // Check for conflicts for the new schedule
        // If it's the same employee, we exclude the current meeting
        var excludeMeetingId = (request.NewEmployeeId == null || request.NewEmployeeId == meet.AssignedEmployeeId)
            ? meet.Id
            : (int?)null;

        var conflict = await _schedulingService.CheckConflictsAsync(
            employeeIdToCheck.Value,
            request.NewStart,
            newEnd,
            excludeMeetingId);

        if (conflict != null)
        {
            _logger.LogWarning(
                "Conflict when rescheduling meeting {MeetingId} to {NewStart}: {ConflictType}",
                id, request.NewStart, conflict.Type);

            return Conflict(new SchedulingConflictResponse
            {
                Error = "SCHEDULING_CONFLICT",
                Message = conflict.Message,
                Conflict = conflict
            });
        }

        // Use transaction for consistency
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Release slots from the previous schedule
            if (meet.AssignedEmployeeId.HasValue)
            {
                await _schedulingService.ReleaseSlotsAsync(meet.Id, OccupancyType.Meeting);
            }

            // Update the meeting
            var oldStart = meet.ScheduledStart;
            var oldEnd = meet.ScheduledEnd;
            var oldEmployeeId = meet.AssignedEmployeeId;

            meet.ScheduledStart = request.NewStart;
            meet.ScheduledEnd = newEnd;
            meet.Status = MeetStatus.Rescheduled;

            if (request.NewEmployeeId.HasValue)
            {
                meet.AssignedEmployeeId = request.NewEmployeeId;
            }

            // Acquire slots for the new schedule
            await _schedulingService.AcquireSlotsAsync(
                employeeIdToCheck.Value,
                request.NewStart,
                newEnd,
                OccupancyType.Meeting,
                meet.Id);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Meeting {MeetingId} rescheduled: {OldStart} -> {NewStart}, employee: {OldEmployee} -> {NewEmployee}",
                id, oldStart, request.NewStart, oldEmployeeId, meet.AssignedEmployeeId);

            return NoContent();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex,
                "Unique constraint violation when rescheduling meeting {MeetingId}",
                id);

            return Conflict(new SchedulingConflictResponse
            {
                Error = "SLOT_ALREADY_TAKEN",
                Message = "The new time slot was reserved by a concurrent operation. Please try again.",
                Conflict = new SchedulingConflict
                {
                    Type = ConflictType.ExistingBooking,
                    Message = "Concurrency conflict detected",
                    EmployeeId = employeeIdToCheck.Value,
                    ConflictStart = request.NewStart,
                    ConflictEnd = newEnd
                }
            });
        }
    }

    /// <summary>
    /// Detects if a database exception is a unique constraint violation.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL: code 23505 = unique_violation
        // SQL Server: code 2601 or 2627 = unique constraint/index violation
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";
        return innerMessage.Contains("unique") || 
               innerMessage.Contains("duplicate") ||
               innerMessage.Contains("23505") ||
               innerMessage.Contains("2601") ||
               innerMessage.Contains("2627");
    }

    [HttpPost("meetings/{id}/adjustment")]
    public async Task<IActionResult> RequestAdjustment(int id, AdjustmentRequest request)
    {
        var meet = await _context.ServiceMeets.FindAsync(id);
        if (meet == null || meet.IsDeleted) return NotFound();

        meet.AdjustmentStatus = AdjustmentStatus.PendingReview;
        meet.AdjustmentAmount = request.Amount;
        meet.AdjustmentReason = request.Reason;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("meetings/{id}/adjustment/approve")]
    public async Task<IActionResult> ApproveAdjustment(int id, [FromQuery] bool approve = true)
    {
        var meet = await _context.ServiceMeets.FindAsync(id);
        if (meet == null || meet.IsDeleted) return NotFound();

        meet.AdjustmentStatus = approve ? AdjustmentStatus.Approved : AdjustmentStatus.Rejected;

        if (!approve)
        {
            meet.AdjustmentAmount = null;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Calendar view
    [HttpGet("calendar")]
    public async Task<ActionResult<IEnumerable<CalendarDayDto>>> GetCalendar(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? serviceAreaId = null)
    {
        var query = _context.ServiceMeets
            .Where(m => !m.IsDeleted)
            .Where(m => m.ScheduledStart >= startDate && m.ScheduledStart <= endDate)
            .Where(m => m.Status != MeetStatus.Cancelled);

        if (serviceAreaId.HasValue)
            query = query.Where(m => m.ServiceAreaId == serviceAreaId.Value);

        var meetings = await query
            .Include(m => m.AssignedEmployee)
            .Include(m => m.ServiceOrder)
            .ToListAsync();

        var calendar = meetings
            .GroupBy(m => m.ScheduledStart.Date)
            .Select(g => new CalendarDayDto
            {
                Date = g.Key,
                TotalMeetings = g.Count(),
                Meetings = g.Select(m => new CalendarMeetingDto
                {
                    Id = m.Id,
                    Start = m.ScheduledStart,
                    End = m.ScheduledEnd,
                    EmployeeName = m.AssignedEmployee != null 
                        ? $"{m.AssignedEmployee.FirstName} {m.AssignedEmployee.LastName}" 
                        : "Unassigned",
                    Address = m.ServiceOrder?.Address ?? "",
                    Status = m.Status
                }).ToList()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(calendar);
    }
}

#region DTOs

public record OrderSummaryDto
{
    public int Id { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public string Address { get; init; } = "";
    public string? City { get; init; }
    public string ZipCode { get; init; } = "";
    public string? ServiceAreaName { get; init; }
    public string? ServiceTypeName { get; init; }
    public decimal Total { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ScheduledDate { get; init; }
}

public record MeetingSummaryDto
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public DateTime? ActualStart { get; init; }
    public DateTime? ActualEnd { get; init; }
    public int? EmployeeId { get; init; }
    public string? EmployeeName { get; init; }
    public string? ServiceAreaName { get; init; }
    public string? Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public MeetStatus Status { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public AdjustmentStatus AdjustmentStatus { get; init; }
    public decimal? AdjustmentAmount { get; init; }
}

public record CalendarDayDto
{
    public DateTime Date { get; init; }
    public int TotalMeetings { get; init; }
    public List<CalendarMeetingDto> Meetings { get; init; } = new();
}

public record CalendarMeetingDto
{
    public int Id { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public string EmployeeName { get; init; } = "";
    public string Address { get; init; } = "";
    public MeetStatus Status { get; init; }
}

public record UpdateOrderStatusRequest(OrderStatus OrderStatus);
public record CancelOrderRequest(string? Reason);
public record UpdateMeetingStatusRequest(MeetStatus Status, string? Notes = null);
public record AssignEmployeeRequest(int EmployeeId);
public record RescheduleMeetingRequest(DateTime NewStart, int? NewEmployeeId = null);
public record AdjustmentRequest(decimal Amount, string Reason);

/// <summary>
/// Scheduling conflict response (HTTP 409)
/// </summary>
public record SchedulingConflictResponse
{
    /// <summary>
    /// Error code for programmatic use
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Descriptive message of the conflict
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Full details of the conflict
    /// </summary>
    public SchedulingConflict? Conflict { get; init; }
}

#endregion
