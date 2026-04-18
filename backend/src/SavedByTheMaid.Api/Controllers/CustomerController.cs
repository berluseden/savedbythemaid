using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Application.DTOs.Orders;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Services;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Controllers;

/// <summary>
/// API for the authenticated customer portal
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerController> _logger;
    private readonly ISchedulingService _schedulingService;
    private readonly IOrderCancellationService _orderCancellationService;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
    private readonly IValidator<CancelOrderRequest> _cancelValidator;
    private readonly IValidator<RescheduleOrderRequest> _rescheduleValidator;

    public CustomerController(
        ApplicationDbContext context,
        ILogger<CustomerController> logger,
        ISchedulingService schedulingService,
        IOrderCancellationService orderCancellationService,
        IStatusHistoryService statusHistoryService,
        IValidator<UpdateProfileRequest> updateProfileValidator,
        IValidator<CancelOrderRequest> cancelValidator,
        IValidator<RescheduleOrderRequest> rescheduleValidator)
    {
        _context = context;
        _logger = logger;
        _schedulingService = schedulingService;
        _orderCancellationService = orderCancellationService;
        _statusHistoryService = statusHistoryService;
        _updateProfileValidator = updateProfileValidator;
        _cancelValidator = cancelValidator;
        _rescheduleValidator = rescheduleValidator;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Gets the current customer's orders
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(CustomerOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerOrdersResponse>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.ServiceOrders
            .AsNoTracking()
            .Include(o => o.ServiceType)
            .Include(o => o.ServiceArea)
            .Include(o => o.CleaningPlace)
            .Include(o => o.Meetings)
            .Where(o => o.CustomerId == userId && !o.IsDeleted);

        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
        {
            query = query.Where(o => o.OrderStatus == orderStatus);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var orderDtos = orders.Select(o => {
            var firstMeeting = o.Meetings.OrderBy(m => m.ScheduledStart).FirstOrDefault();
            return new CustomerOrderDto
            {
                Id = o.Id,
                ServiceTypeName = o.ServiceType?.Name ?? "N/A",
                ServiceAreaName = o.ServiceArea?.Name ?? "N/A",
                CleaningPlaceName = o.CleaningPlace?.Name ?? "N/A",
                Address = o.Address,
                City = o.City ?? "",
                ZipCode = o.ZipCode,
                ScheduledDate = firstMeeting != null ? DateOnly.FromDateTime(firstMeeting.ScheduledStart) : null,
                ScheduledTime = firstMeeting != null ? TimeOnly.FromDateTime(firstMeeting.ScheduledStart) : null,
                EstimatedDuration = o.EstimatedDurationMinutes,
                TotalAmount = o.Total,
                Status = o.OrderStatus.ToString(),
                RecurrenceType = o.RecurrenceType.ToString(),
                CreatedAt = o.CreatedAt,
                SpecialInstructions = o.SpecialInstructions
            };
        }).ToList();

        return Ok(new CustomerOrdersResponse
        {
            Items = orderDtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    /// <summary>
    /// Gets a specific customer order
    /// </summary>
    [HttpGet("my-orders/{id}")]
    [ProducesResponseType(typeof(CustomerOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOrderDetailDto>> GetOrderDetail(int id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _context.ServiceOrders
            .AsNoTracking()
            .Include(o => o.ServiceType)
            .Include(o => o.ServiceArea)
            .Include(o => o.CleaningPlace)
            .Include(o => o.Items)
                .ThenInclude(i => i.AdditionalServiceType)
            .Include(o => o.Rooms)
                .ThenInclude(r => r.CleaningPlaceRoom)
            .Include(o => o.Meetings)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId && !o.IsDeleted, cancellationToken);

        if (order == null)
            return NotFound();

        var firstMeeting = order.Meetings.OrderBy(m => m.ScheduledStart).FirstOrDefault();

        return Ok(new CustomerOrderDetailDto
        {
            Id = order.Id,
            ServiceTypeName = order.ServiceType?.Name ?? "N/A",
            ServiceAreaName = order.ServiceArea?.Name ?? "N/A",
            CleaningPlaceName = order.CleaningPlace?.Name ?? "N/A",
            Address = order.Address,
            City = order.City ?? "",
            State = order.State ?? "",
            ZipCode = order.ZipCode,
            ScheduledDate = firstMeeting != null ? DateOnly.FromDateTime(firstMeeting.ScheduledStart) : null,
            ScheduledTime = firstMeeting != null ? TimeOnly.FromDateTime(firstMeeting.ScheduledStart) : null,
            EstimatedDuration = order.EstimatedDurationMinutes,
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            Tax = order.Tax,
            TotalAmount = order.Total,
            Status = order.OrderStatus.ToString(),
            RecurrenceType = order.RecurrenceType.ToString(),
            SpecialInstructions = order.SpecialInstructions,
            CreatedAt = order.CreatedAt,
            Rooms = order.Rooms.Select(r => new OrderRoomDto
            {
                RoomName = r.CleaningPlaceRoom?.Name ?? "N/A",
                Quantity = r.Quantity,
                Price = r.CalculatedPrice
            }).ToList(),
            AdditionalServices = order.Items.Where(i => i.AdditionalServiceType != null).Select(s => new OrderAdditionalServiceDto
            {
                ServiceName = s.AdditionalServiceType?.Title ?? "N/A",
                Price = s.Total
            }).ToList()
        });
    }

    /// <summary>
    /// Gets customer statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CustomerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var orders = _context.ServiceOrders
            .AsNoTracking()
            .Where(o => o.CustomerId == userId && !o.IsDeleted);

        var completedOrders = await orders.CountAsync(o => o.OrderStatus == OrderStatus.Completed, cancellationToken);
        var totalSpent = await orders.Where(o => o.OrderStatus == OrderStatus.Completed).SumAsync(o => o.Total, cancellationToken);

        var now = DateTime.UtcNow;
        var nextMeeting = await _context.ServiceMeets
            .AsNoTracking()
            .Where(m => m.ServiceOrder != null
                && m.ServiceOrder.CustomerId == userId
                && !m.ServiceOrder.IsDeleted
                && (m.ServiceOrder.OrderStatus == OrderStatus.PendingReview || m.ServiceOrder.OrderStatus == OrderStatus.Confirmed)
                && m.ScheduledStart >= now)
            .OrderBy(m => m.ScheduledStart)
            .Select(m => (DateTime?)m.ScheduledStart)
            .FirstOrDefaultAsync(cancellationToken);

        string? nextBookingDate = null;
        if (nextMeeting.HasValue)
        {
            nextBookingDate = nextMeeting.Value.ToString("yyyy-MM-dd");
        }

        return Ok(new CustomerStatsDto
        {
            CompletedBookings = completedOrders,
            TotalSpent = totalSpent,
            NextBooking = nextBookingDate,
            LoyaltyPoints = completedOrders * 50 // 50 points per completed order
        });
    }

    /// <summary>
    /// Cancels a customer order (only if pending or confirmed).
    /// IDOR check only — full status validation lives in the service.
    /// </summary>
    [HttpPost("my-orders/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _cancelValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // IDOR check: ensure the order belongs to this customer
        var exists = await _context.ServiceOrders
            .AsNoTracking()
            .AnyAsync(o => o.Id == id && o.CustomerId == userId && !o.IsDeleted, cancellationToken);

        if (!exists)
            return NotFound();

        var reason = $"Cancelled by customer: {request.Reason}";
        var (success, error) = await _orderCancellationService.CancelOrderAsync(id, reason, userId);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }

    /// <summary>
    /// Reschedules an order to a new date/time (only if confirmed, meetings must be reschedulable).
    /// Validates availability via SchedulingService and updates SlotOccupancies atomically.
    /// </summary>
    [HttpPost("my-orders/{id}/reschedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RescheduleOrder(int id, [FromBody] RescheduleOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _rescheduleValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _context.ServiceOrders
            .Include(o => o.Meetings)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId && !o.IsDeleted, cancellationToken);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != OrderStatus.Confirmed)
            return BadRequest("Only confirmed orders can be rescheduled");

        // Parse new date and time
        if (!DateOnly.TryParse(request.NewDate, out var newDate))
            return BadRequest("Invalid date format");

        if (!TimeOnly.TryParse(request.NewTime, out var newTime))
            return BadRequest("Invalid time format");

        // Check if date is in the future
        var newDateTime = newDate.ToDateTime(newTime, DateTimeKind.Utc);
        if (newDateTime <= DateTime.UtcNow)
            return BadRequest("New date/time must be in the future");

        // Capture previous statuses before mutation for the audit trail
        var previousMeetStatuses = order.Meetings.ToDictionary(m => m.Id, m => m.Status);

        // Validate each meeting can be rescheduled
        var reschedulableMeetings = new List<(Domain.Entities.ServiceMeet meeting, DateTime newStart, DateTime newEnd)>();
        foreach (var meeting in order.Meetings)
        {
            // Reject meetings in terminal or in-progress states
            if (meeting.Status == MeetStatus.InProgress ||
                meeting.Status == MeetStatus.Completed ||
                meeting.Status == MeetStatus.Cancelled ||
                meeting.Status == MeetStatus.NoShow)
            {
                return BadRequest($"Meeting {meeting.Id} cannot be rescheduled because its status is {meeting.Status}");
            }

            var originalDuration = meeting.ScheduledEnd - meeting.ScheduledStart;
            var newEnd = newDateTime.Add(originalDuration);
            reschedulableMeetings.Add((meeting, newDateTime, newEnd));
        }

        // Conflict check and slot swap within a transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var (meeting, newStart, newEnd) in reschedulableMeetings)
            {
                if (!meeting.AssignedEmployeeId.HasValue)
                {
                    // No employee assigned yet — just update times
                    meeting.ScheduledStart = newStart;
                    meeting.ScheduledEnd = newEnd;
                    meeting.Status = MeetStatus.Rescheduled;
                    continue;
                }

                var employeeId = meeting.AssignedEmployeeId.Value;

                // (a) Release old slots
                await _schedulingService.ReleaseSlotsAsync(meeting.Id, OccupancyType.Meeting);

                // (b) Check conflicts for the new slot (excluding this meeting)
                var conflict = await _schedulingService.CheckConflictsAsync(
                    employeeId, newStart, newEnd, excludeMeetingId: meeting.Id);

                if (conflict != null)
                {
                    // Rollback will restore old slots since they were in the same transaction
                    await transaction.RollbackAsync();
                    return Conflict(new { message = $"The requested time slot is not available. {conflict.Message}" });
                }

                // (c) Acquire slots for the new time
                await _schedulingService.AcquireSlotsAsync(
                    employeeId, newStart, newEnd, OccupancyType.Meeting, meeting.Id);

                // (d) Update meeting times and status
                meeting.ScheduledStart = newStart;
                meeting.ScheduledEnd = newEnd;
                meeting.Status = MeetStatus.Rescheduled;
            }

            order.SpecialInstructions = string.IsNullOrEmpty(order.SpecialInstructions)
                ? $"Rescheduled by customer to {newDate:MMM d, yyyy} at {newTime:hh:mm tt}"
                : $"{order.SpecialInstructions}\n\nRescheduled by customer to {newDate:MMM d, yyyy} at {newTime:hh:mm tt}";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to reschedule order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while rescheduling. Please try again." });
        }

        // Record audit trail outside the transaction — a failure here must not
        // roll back the already-committed reschedule or return 500 to the customer.
        try
        {
            foreach (var (meeting, _, _) in reschedulableMeetings)
            {
                var previousStatus = previousMeetStatuses.GetValueOrDefault(meeting.Id, MeetStatus.Scheduled);
                await _statusHistoryService.RecordMeetStatusChangeAsync(
                    meeting.Id,
                    previousStatus,
                    MeetStatus.Rescheduled,
                    userId,
                    reasonCode: "CUSTOMER_RESCHEDULE",
                    notes: $"Rescheduled by customer to {newDate:yyyy-MM-dd} at {newTime:HH:mm}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit trail for reschedule of order {OrderId}", id);
        }

        _logger.LogInformation("Order {OrderId} rescheduled by customer {UserId} to {NewDate} {NewTime}",
            id, userId, request.NewDate, request.NewTime);

        return Ok(new { message = "Booking rescheduled successfully" });
    }

    /// <summary>
    /// Gets the customer's profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileDto>> GetProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound();

        return Ok(new CustomerProfileDto
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            Address = user.Address ?? "",
            City = user.City ?? "",
            State = user.State ?? "",
            ZipCode = user.ZipCode ?? ""
        });
    }

    /// <summary>
    /// Updates the customer's profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateProfileValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound();

        // Update fields
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.Phone;
        user.Address = request.Address;
        user.City = request.City;
        user.State = request.State;
        user.ZipCode = request.ZipCode;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(new { message = "Profile updated successfully" });
    }
}

#region DTOs

public record CustomerOrdersResponse
{
    public List<CustomerOrderDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public record CustomerOrderDto
{
    public int Id { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string ServiceAreaName { get; set; } = string.Empty;
    public string CleaningPlaceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public int EstimatedDuration { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? SpecialInstructions { get; set; }
}

public record CustomerOrderDetailDto : CustomerOrderDto
{
    public string State { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public List<OrderRoomDto> Rooms { get; set; } = new();
    public List<OrderAdditionalServiceDto> AdditionalServices { get; set; } = new();
}

public record OrderRoomDto
{
    public string RoomName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public record OrderAdditionalServiceDto
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public record CustomerStatsDto
{
    public int CompletedBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public string? NextBooking { get; set; }
    public int LoyaltyPoints { get; set; }
}

public record CustomerProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public record UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

#endregion
