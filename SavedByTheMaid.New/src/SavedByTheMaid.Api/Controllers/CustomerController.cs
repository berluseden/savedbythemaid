using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

/// <summary>
/// API para el portal del cliente autenticado
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ApplicationDbContext context, ILogger<CustomerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Obtiene las órdenes del cliente actual
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<ActionResult<CustomerOrdersResponse>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.ServiceOrders
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

        var totalItems = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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
                PaymentStatus = o.PaymentStatus.ToString(),
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
    /// Obtiene una orden específica del cliente
    /// </summary>
    [HttpGet("my-orders/{id}")]
    public async Task<ActionResult<CustomerOrderDetailDto>> GetOrderDetail(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _context.ServiceOrders
            .Include(o => o.ServiceType)
            .Include(o => o.ServiceArea)
            .Include(o => o.CleaningPlace)
            .Include(o => o.Items)
                .ThenInclude(i => i.AdditionalServiceType)
            .Include(o => o.Rooms)
                .ThenInclude(r => r.CleaningPlaceRoom)
            .Include(o => o.Meetings)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId && !o.IsDeleted);

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
            PaymentStatus = order.PaymentStatus.ToString(),
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
    /// Obtiene estadísticas del cliente
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<CustomerStatsDto>> GetStats()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var orders = await _context.ServiceOrders
            .Include(o => o.Meetings)
            .Where(o => o.CustomerId == userId && !o.IsDeleted)
            .ToListAsync();

        var completedOrders = orders.Count(o => o.OrderStatus == OrderStatus.Completed);
        var totalSpent = orders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.Total);
        
        // Find next upcoming meeting
        var now = DateTime.Now;
        var nextMeeting = orders
            .Where(o => o.OrderStatus == OrderStatus.Draft || o.OrderStatus == OrderStatus.Confirmed)
            .SelectMany(o => o.Meetings)
            .Where(m => m.ScheduledStart >= now)
            .OrderBy(m => m.ScheduledStart)
            .FirstOrDefault();

        string? nextBookingDate = null;
        if (nextMeeting != null)
        {
            nextBookingDate = nextMeeting.ScheduledStart.ToString("yyyy-MM-dd");
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
    /// Cancela una orden del cliente (solo si está pendiente o confirmada)
    /// </summary>
    [HttpPost("my-orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CustomerCancelRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _context.ServiceOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId && !o.IsDeleted);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != OrderStatus.Draft && order.OrderStatus != OrderStatus.Confirmed)
            return BadRequest("Only pending or confirmed orders can be cancelled");

        order.OrderStatus = OrderStatus.Cancelled;
        order.SpecialInstructions = string.IsNullOrEmpty(order.SpecialInstructions)
            ? $"Cancelled by customer: {request.Reason}"
            : $"{order.SpecialInstructions}\n\nCancelled by customer: {request.Reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} cancelled by customer {UserId}", id, userId);

        return NoContent();
    }
}

#region DTOs

public class CustomerOrdersResponse
{
    public List<CustomerOrderDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class CustomerOrderDto
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
    public string PaymentStatus { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class CustomerOrderDetailDto : CustomerOrderDto
{
    public string State { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public List<OrderRoomDto> Rooms { get; set; } = new();
    public List<OrderAdditionalServiceDto> AdditionalServices { get; set; } = new();
}

public class OrderRoomDto
{
    public string RoomName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderAdditionalServiceDto
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CustomerStatsDto
{
    public int CompletedBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public string? NextBooking { get; set; }
    public int LoyaltyPoints { get; set; }
}

public class CustomerCancelRequest
{
    public string Reason { get; set; } = string.Empty;
}

#endregion
