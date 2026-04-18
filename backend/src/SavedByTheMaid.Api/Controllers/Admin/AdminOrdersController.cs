using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Application.DTOs.Orders;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Services;

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
    private readonly IOrderCancellationService _orderCancellationService;

    public AdminOrdersController(
        ApplicationDbContext context,
        ILogger<AdminOrdersController> logger,
        ISchedulingService schedulingService,
        IStatusHistoryService statusHistoryService,
        IOrderCancellationService orderCancellationService)
    {
        _context = context;
        _logger = logger;
        _schedulingService = schedulingService;
        _statusHistoryService = statusHistoryService;
        _orderCancellationService = orderCancellationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetAll(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? serviceAreaId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
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
                ScheduledDate = o.Meetings
                    .OrderBy(m => m.ScheduledStart)
                    .Select(m => (DateTime?)m.ScheduledStart)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(items, totalCount, page, pageSize);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.ServiceOrders
            .AsNoTracking()
            .Where(o => o.Id == id && !o.IsDeleted)
            .Select(o => new
            {
                o.Id,
                ConfirmationNumber = $"SBM-{o.Id:D6}",
                o.ContactName,
                o.ContactPhone,
                o.Address,
                o.City,
                o.State,
                o.ZipCode,
                o.SpecialInstructions,
                o.OrderStatus,
                o.RecurrenceType,
                o.EstimatedDurationMinutes,
                o.Subtotal,
                o.Discount,
                o.Tax,
                o.Total,
                o.CreatedAt,
                o.IsDeleted,
                Customer = o.Customer == null ? null : new
                {
                    o.Customer.Id,
                    o.Customer.Email,
                    o.Customer.FirstName,
                    o.Customer.LastName,
                    o.Customer.PhoneNumber,
                    o.Customer.IsActive,
                    o.Customer.EmailConfirmed,
                    o.Customer.CreatedAt
                    // PasswordHash, SecurityStamp, ConcurrencyStamp intentionally excluded
                },
                ServiceArea = o.ServiceArea == null ? null : new { o.ServiceArea.Id, o.ServiceArea.Name },
                ServiceType = o.ServiceType == null ? null : new { o.ServiceType.Id, o.ServiceType.Name },
                CleaningPlace = o.CleaningPlace == null ? null : new { o.CleaningPlace.Id, o.CleaningPlace.Name },
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.Quantity,
                    i.UnitPrice,
                    i.Total,
                    AdditionalServiceType = i.AdditionalServiceType == null ? null : new
                    {
                        i.AdditionalServiceType.Id,
                        i.AdditionalServiceType.Title
                    }
                }).ToList(),
                Rooms = o.Rooms.Select(r => new
                {
                    r.Id,
                    r.Quantity,
                    r.CalculatedPrice,
                    CleaningPlaceRoom = r.CleaningPlaceRoom == null ? null : new
                    {
                        r.CleaningPlaceRoom.Id,
                        r.CleaningPlaceRoom.Name
                    }
                }).ToList(),
                Meetings = o.Meetings.Select(m => new
                {
                    m.Id,
                    m.ScheduledStart,
                    m.ScheduledEnd,
                    m.ActualStart,
                    m.ActualEnd,
                    m.Status,
                    m.EstimatedDurationMinutes,
                    m.Notes,
                    m.AdjustmentStatus,
                    m.AdjustmentAmount,
                    AssignedEmployee = m.AssignedEmployee == null ? null : new
                    {
                        m.AssignedEmployee.Id,
                        m.AssignedEmployee.FirstName,
                        m.AssignedEmployee.LastName
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return order == null ? NotFound() : Ok(order);
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.ServiceOrders.FindAsync(new object[] { id }, cancellationToken);
        if (order == null || order.IsDeleted) return NotFound();

        // Validate allowed OrderStatus transitions
        var currentStatus = order.OrderStatus;
        var newStatus = request.OrderStatus;

        // Validate transition using domain state machine
        var transitionResult = OrderStatusTransitions.Validate(currentStatus, newStatus);
        if (transitionResult.IsFailure)
        {
            return BadRequest(new { message = transitionResult.Error.Description });
        }

        _logger.LogInformation("Order {OrderId} status transition: {From} -> {To}", 
            id, currentStatus, newStatus);

        order.OrderStatus = newStatus;
        await _context.SaveChangesAsync(cancellationToken);

        // Record audit trail
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _statusHistoryService.RecordOrderStatusChangeAsync(
            id, currentStatus, newStatus, userId, notes: $"Admin status change: {currentStatus} -> {newStatus}");

        return NoContent();
    }

    // POST is intentional here — cancel is a non-idempotent state transition that may trigger
    // side-effects (notifications, refunds). Using DELETE would imply hard-deletion.
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        int id,
        [FromBody] CancelOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var reason = request?.Reason ?? "Order cancelled by admin";

        var previousStatus = await _context.ServiceOrders
            .Where(o => o.Id == id && !o.IsDeleted)
            .Select(o => (OrderStatus?)o.OrderStatus)
            .FirstOrDefaultAsync(cancellationToken);

        var (success, error) = await _orderCancellationService.CancelOrderAsync(id, reason, adminId);
        if (!success)
            return NotFound(new { message = error });

        await _statusHistoryService.RecordOrderStatusChangeAsync(
            id, previousStatus, OrderStatus.Cancelled, adminId,
            reasonCode: "ADMIN_CANCEL", notes: reason);

        return NoContent();
    }

    // Meetings (Appointments)
    [HttpGet("meetings")]
    [ProducesResponseType(typeof(IEnumerable<MeetingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingSummaryDto>>> GetMeetings(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? serviceAreaId = null,
        [FromQuery] MeetStatus? status = null,
        [FromQuery] int? orderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
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
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    [HttpGet("meetings/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetMeetingById(int id, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets
            .AsNoTracking()
            .Where(m => m.Id == id && !m.IsDeleted)
            .Select(m => new
            {
                m.Id,
                m.ServiceOrderId,
                ConfirmationNumber = $"SBM-{m.ServiceOrderId:D6}",
                m.ScheduledStart,
                m.ScheduledEnd,
                m.ActualStart,
                m.ActualEnd,
                m.Status,
                m.EstimatedDurationMinutes,
                m.Notes,
                m.AdjustmentStatus,
                m.AdjustmentAmount,
                m.AdjustmentReason,
                ServiceArea = m.ServiceArea == null ? null : new { m.ServiceArea.Id, m.ServiceArea.Name },
                AssignedEmployee = m.AssignedEmployee == null ? null : new
                {
                    m.AssignedEmployee.Id,
                    m.AssignedEmployee.FirstName,
                    m.AssignedEmployee.LastName
                },
                ServiceOrder = m.ServiceOrder == null ? null : new
                {
                    m.ServiceOrder.Id,
                    m.ServiceOrder.ContactName,
                    m.ServiceOrder.ContactPhone,
                    m.ServiceOrder.Address,
                    m.ServiceOrder.City,
                    m.ServiceOrder.ZipCode,
                    m.ServiceOrder.OrderStatus,
                    m.ServiceOrder.Total,
                    Items = m.ServiceOrder.Items.Select(i => new
                    {
                        i.Id,
                        i.Quantity,
                        i.UnitPrice,
                        i.Total
                    }).ToList()
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return meet == null ? NotFound() : Ok(meet);
    }

    [HttpPut("meetings/{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMeetingStatus(int id, UpdateMeetingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets.FindAsync(new object[] { id }, cancellationToken);
        if (meet == null || meet.IsDeleted) return NotFound();

        var previousStatus = meet.Status;

        // Validate transition using domain state machine
        var meetTransitionResult = MeetStatusTransitions.Validate(previousStatus, request.Status);
        if (meetTransitionResult.IsFailure)
        {
            return BadRequest(new { message = meetTransitionResult.Error.Description });
        }

        meet.Status = request.Status;

        if (request.Status == MeetStatus.InProgress && !meet.ActualStart.HasValue)
            meet.ActualStart = DateTime.UtcNow;

        if (request.Status == MeetStatus.Completed && !meet.ActualEnd.HasValue)
            meet.ActualEnd = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
            meet.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        // Record audit trail
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _statusHistoryService.RecordMeetStatusChangeAsync(
            id, previousStatus, request.Status, adminId, notes: request.Notes);

        return NoContent();
    }

    [HttpPut("meetings/{id}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignEmployee(int id, AssignEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets.FindAsync(new object[] { id }, cancellationToken);
        if (meet == null || meet.IsDeleted) return NotFound();

        var employee = await _context.Employees.FindAsync(new object[] { request.EmployeeId }, cancellationToken);
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
                s.IsAvailable && !s.IsDeleted, cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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

    [HttpPut("meetings/{id}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleMeeting(int id, RescheduleMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets
            .Include(m => m.AssignedEmployee)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
            
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

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestAdjustment(int id, AdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets.FindAsync(new object[] { id }, cancellationToken);
        if (meet == null || meet.IsDeleted) return NotFound();

        meet.AdjustmentStatus = AdjustmentStatus.PendingReview;
        meet.AdjustmentAmount = request.Amount;
        meet.AdjustmentReason = request.Reason;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("meetings/{id}/adjustment/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveAdjustment(int id, [FromQuery] bool approve = true, CancellationToken cancellationToken = default)
    {
        var meet = await _context.ServiceMeets.FindAsync(new object[] { id }, cancellationToken);
        if (meet == null || meet.IsDeleted) return NotFound();

        meet.AdjustmentStatus = approve ? AdjustmentStatus.Approved : AdjustmentStatus.Rejected;

        if (!approve)
        {
            meet.AdjustmentAmount = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Calendar view
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IEnumerable<CalendarDayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CalendarDayDto>>> GetCalendar(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? serviceAreaId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceMeets
            .Where(m => !m.IsDeleted)
            .Where(m => m.ScheduledStart >= startDate && m.ScheduledStart <= endDate)
            .Where(m => m.Status != MeetStatus.Cancelled);

        if (serviceAreaId.HasValue)
            query = query.Where(m => m.ServiceAreaId == serviceAreaId.Value);

        var meetings = await query
            .AsNoTracking()
            .Include(m => m.AssignedEmployee)
            .Include(m => m.ServiceOrder)
            .ToListAsync(cancellationToken);

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

public record RescheduleMeetingRequest(DateTime NewStart, int? NewEmployeeId = null);
public record AdjustmentRequest(decimal Amount, string Reason);

/// <summary>
/// Scheduling conflict response (HTTP 409)
/// </summary>
/// <summary>
/// Generic paged result wrapper for list endpoints.
/// </summary>
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

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
