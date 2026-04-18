using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

public interface IOrderCancellationService
{
    Task<(bool Success, string? Error)> CancelOrderAsync(int orderId, string? reason, string? cancelledById);
}

public class OrderCancellationService : IOrderCancellationService
{
    private readonly ApplicationDbContext _context;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<OrderCancellationService> _logger;

    public OrderCancellationService(
        ApplicationDbContext context,
        ISchedulingService schedulingService,
        ILogger<OrderCancellationService> logger)
    {
        _context = context;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> CancelOrderAsync(int orderId, string? reason, string? cancelledById)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Meetings)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

        if (order == null)
            return (false, "Order not found");

        if (order.OrderStatus == OrderStatus.Completed ||
            order.OrderStatus == OrderStatus.Cancelled ||
            order.OrderStatus == OrderStatus.NoShow)
        {
            return (false, $"Cannot cancel an order with status {order.OrderStatus}");
        }

        order.OrderStatus = OrderStatus.Cancelled;

        if (!string.IsNullOrEmpty(reason))
        {
            order.SpecialInstructions = string.IsNullOrEmpty(order.SpecialInstructions)
                ? $"Cancelled: {reason}"
                : $"{order.SpecialInstructions}\n\nCancelled: {reason}";
        }

        // Cascade: cancel all pending meetings and release their slots
        var cancelledMeetCount = 0;
        foreach (var meet in order.Meetings.Where(m =>
            m.Status == MeetStatus.Scheduled ||
            m.Status == MeetStatus.Assigned ||
            m.Status == MeetStatus.Rescheduled))
        {
            meet.Status = MeetStatus.Cancelled;
            meet.CancellationReason = reason ?? "Order cancelled";

            if (meet.AssignedEmployeeId.HasValue)
            {
                await _schedulingService.ReleaseSlotsAsync(meet.Id, OccupancyType.Meeting);
            }

            cancelledMeetCount++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Order {OrderId} cancelled by {CancelledById} - {MeetCount} meetings cancelled",
            orderId, cancelledById ?? "unknown", cancelledMeetCount);

        return (true, null);
    }
}
