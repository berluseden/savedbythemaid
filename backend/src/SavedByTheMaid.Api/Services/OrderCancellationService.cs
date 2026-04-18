using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Services;
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
    private readonly IEmailService _emailService;
    private readonly IStatusHistoryService _statusHistoryService;
    private readonly ILogger<OrderCancellationService> _logger;

    public OrderCancellationService(
        ApplicationDbContext context,
        ISchedulingService schedulingService,
        IEmailService emailService,
        IStatusHistoryService statusHistoryService,
        ILogger<OrderCancellationService> logger)
    {
        _context = context;
        _schedulingService = schedulingService;
        _emailService = emailService;
        _statusHistoryService = statusHistoryService;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> CancelOrderAsync(int orderId, string? reason, string? cancelledById)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Customer)
            .Include(o => o.ServiceType)
            .Include(o => o.Meetings)
                .ThenInclude(m => m.AssignedEmployee)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

        if (order == null)
            return (false, "Order not found");

        // Validate the order-level transition via state machine before writing anything
        var previousOrderStatus = order.OrderStatus;
        var orderTransition = OrderStatusTransitions.Validate(previousOrderStatus, OrderStatus.Cancelled);
        if (!orderTransition.IsSuccess)
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
        var meetingsToNotify = new List<(ServiceMeet Meet, MeetStatus PreviousStatus)>();
        foreach (var meet in order.Meetings.Where(m =>
            m.Status == MeetStatus.Scheduled ||
            m.Status == MeetStatus.Assigned ||
            m.Status == MeetStatus.Rescheduled))
        {
            // Validate the meet-level transition via state machine before writing
            var previousMeetStatus = meet.Status;
            var meetTransition = MeetStatusTransitions.Validate(previousMeetStatus, MeetStatus.Cancelled);
            if (!meetTransition.IsSuccess)
            {
                _logger.LogWarning(
                    "Skipping meet {MeetId} cancellation: invalid transition {From} -> Cancelled",
                    meet.Id, previousMeetStatus);
                continue;
            }

            meet.Status = MeetStatus.Cancelled;
            meet.CancellationReason = reason ?? "Order cancelled";

            if (meet.AssignedEmployeeId.HasValue)
            {
                await _schedulingService.ReleaseSlotsAsync(meet.Id, OccupancyType.Meeting);
            }

            // Store the previous status alongside the meet for audit trail recording after SaveChanges
            meetingsToNotify.Add((meet, previousMeetStatus));
            cancelledMeetCount++;
        }

        await _context.SaveChangesAsync();

        // Record audit trail for the order status change
        await _statusHistoryService.RecordOrderStatusChangeAsync(
            orderId,
            previousOrderStatus,
            OrderStatus.Cancelled,
            cancelledById,
            "ADMIN_CANCEL",
            reason);

        // Record audit trail for each cancelled meeting
        foreach (var (meet, prevStatus) in meetingsToNotify)
        {
            await _statusHistoryService.RecordMeetStatusChangeAsync(
                meet.Id,
                prevStatus,
                MeetStatus.Cancelled,
                cancelledById,
                "ADMIN_CANCEL",
                reason);
        }

        _logger.LogInformation(
            "Order {OrderId} cancelled by {CancelledById} - {MeetCount} meetings cancelled",
            orderId, cancelledById ?? "unknown", cancelledMeetCount);

        // Send email notifications after successful save — fire-and-forget, never revert cancellation on failure
        _ = Task.Run(async () =>
        {
            // Notify customer
            var customerEmail = order.Customer?.Email ?? order.ContactEmail;
            var customerName = order.Customer?.FullName ?? order.ContactName ?? "Customer";
            var serviceTypeName = order.ServiceType?.Name ?? "Cleaning Service";
            var firstMeeting = meetingsToNotify.OrderBy(t => t.Meet.ScheduledStart).Select(t => t.Meet).FirstOrDefault();
            var scheduledDate = firstMeeting?.ScheduledStart ?? DateTime.UtcNow;

            if (!string.IsNullOrEmpty(customerEmail))
            {
                try
                {
                    await _emailService.SendBookingCancelledAsync(customerEmail, new BookingCancelledEmail(
                        CustomerName: customerName,
                        ServiceType: serviceTypeName,
                        ScheduledDate: scheduledDate,
                        Reason: reason ?? ""
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation email to customer {Email}", customerEmail);
                }
            }

            // Notify each assigned employee
            foreach (var (meet, _) in meetingsToNotify)
            {
                var employee = meet.AssignedEmployee;
                if (employee == null || string.IsNullOrEmpty(employee.Email))
                    continue;

                try
                {
                    await _emailService.SendOrderCancelledToEmployeeAsync(employee.Email, new OrderCancelledEmployeeEmail(
                        EmployeeName: employee.FullName,
                        CustomerName: customerName,
                        ServiceType: serviceTypeName,
                        ScheduledDate: meet.ScheduledStart,
                        Address: order.Address,
                        Reason: reason
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation email to employee {EmployeeId}", employee.Id);
                }
            }
        });

        return (true, null);
    }
}
