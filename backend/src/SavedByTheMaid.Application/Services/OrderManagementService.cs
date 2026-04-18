using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Application.DTOs.Orders;
using SavedByTheMaid.Application.Interfaces;
using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Errors;
using SavedByTheMaid.Domain.Services;

namespace SavedByTheMaid.Application.Services;

/// <summary>
/// Application service for order and meeting management operations.
/// Extracts business logic from AdminOrdersController and BookingController.
/// All methods return Result or Result{T}, never throw exceptions for business errors.
/// </summary>
public class OrderManagementService : IOrderManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ISchedulingServiceAdapter _schedulingService;
    private readonly IOrderCancellationServiceAdapter _cancellationService;
    private readonly IStatusHistoryServiceAdapter _statusHistoryService;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(
        IApplicationDbContext context,
        ISchedulingServiceAdapter schedulingService,
        IOrderCancellationServiceAdapter cancellationService,
        IStatusHistoryServiceAdapter statusHistoryService,
        ILogger<OrderManagementService> logger)
    {
        _context = context;
        _schedulingService = schedulingService;
        _cancellationService = cancellationService;
        _statusHistoryService = statusHistoryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> CancelOrderAsync(int orderId, CancelOrderRequest request, string? cancelledById = null)
    {
        try
        {
            var reason = request.Reason ?? "Order cancelled";

            var (success, error) = await _cancellationService.CancelOrderAsync(orderId, reason, cancelledById);
            if (!success)
            {
                _logger.LogWarning("Failed to cancel order {OrderId}: {Error}", orderId, error);
                return Result.Failure(new Error("Order.CancelFailed", error ?? "Failed to cancel order."));
            }

            // Record audit trail
            await _statusHistoryService.RecordOrderStatusChangeAsync(
                orderId,
                null, // fromStatus unknown after delegation
                OrderStatus.Cancelled,
                cancelledById,
                reasonCode: "CANCEL",
                notes: reason);

            _logger.LogInformation("Order {OrderId} cancelled by {CancelledById}", orderId, cancelledById ?? "unknown");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error cancelling order {OrderId}", orderId);
            return Result.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, string? changedById = null)
    {
        try
        {
            var order = await _context.ServiceOrders.FindAsync(orderId);
            if (order == null || order.IsDeleted)
            {
                return Result.Failure(OrderErrors.NotFound);
            }

            var currentStatus = order.OrderStatus;
            var newStatus = request.OrderStatus;

            var transitionResult = OrderStatusTransitions.Validate(currentStatus, newStatus);
            if (transitionResult.IsFailure)
            {
                _logger.LogWarning(
                    "Invalid order status transition attempted: {OrderId} from {From} to {To}",
                    orderId, currentStatus, newStatus);
                return transitionResult;
            }

            _logger.LogInformation(
                "Order {OrderId} status transition: {From} -> {To} by {ChangedBy}",
                orderId, currentStatus, newStatus, changedById ?? "unknown");

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            // Record audit trail
            await _statusHistoryService.RecordOrderStatusChangeAsync(
                orderId,
                currentStatus,
                newStatus,
                changedById,
                notes: $"Status change: {currentStatus} -> {newStatus}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating status for order {OrderId}", orderId);
            return Result.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateMeetingStatusAsync(int meetingId, UpdateMeetingStatusRequest request, string? changedById = null)
    {
        try
        {
            var meet = await _context.ServiceMeets.FindAsync(meetingId);
            if (meet == null || meet.IsDeleted)
            {
                return Result.Failure(OrderErrors.MeetingNotFound(meetingId));
            }

            var previousStatus = meet.Status;
            var newStatus = request.Status;

            var meetTransitionResult = MeetStatusTransitions.Validate(previousStatus, newStatus);
            if (meetTransitionResult.IsFailure)
            {
                _logger.LogWarning(
                    "Invalid meeting status transition attempted: {MeetingId} from {From} to {To}",
                    meetingId, previousStatus, newStatus);
                return meetTransitionResult;
            }

            // Apply side effects based on new status
            if (newStatus == MeetStatus.InProgress && !meet.ActualStart.HasValue)
            {
                meet.ActualStart = DateTime.UtcNow;
            }

            if (newStatus == MeetStatus.Completed && !meet.ActualEnd.HasValue)
            {
                meet.ActualEnd = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(request.Notes))
            {
                meet.Notes = request.Notes;
            }

            meet.Status = newStatus;
            await _context.SaveChangesAsync();

            // Record audit trail
            await _statusHistoryService.RecordMeetStatusChangeAsync(
                meetingId,
                previousStatus,
                newStatus,
                changedById,
                notes: request.Notes);

            _logger.LogInformation(
                "Meeting {MeetingId} status transition: {From} -> {To} by {ChangedBy}",
                meetingId, previousStatus, newStatus, changedById ?? "unknown");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating status for meeting {MeetingId}", meetingId);
            return Result.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result> AssignEmployeeAsync(int meetingId, AssignEmployeeRequest request)
    {
        try
        {
            var meet = await _context.ServiceMeets.FindAsync(meetingId);
            if (meet == null || meet.IsDeleted)
            {
                return Result.Failure(OrderErrors.MeetingNotFound(meetingId));
            }

            // Validate employee exists and is active
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
            {
                return Result.Failure(EmployeeErrors.NotFound);
            }

            if (!employee.IsActive)
            {
                return Result.Failure(EmployeeErrors.Inactive);
            }

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
            {
                return Result.Failure(EmployeeErrors.NotAvailableOnDay(request.EmployeeId, meetDayOfWeek));
            }

            if (meetStartTime < empSchedule.StartTime || meetEndTime > empSchedule.EndTime)
            {
                return Result.Failure(EmployeeErrors.OutsideWorkingHours(request.EmployeeId));
            }

            // Check for conflicts
            var conflict = await _schedulingService.CheckConflictsAsync(
                request.EmployeeId,
                meet.ScheduledStart,
                meet.ScheduledEnd,
                excludeMeetingId: null);

            if (conflict != null)
            {
                _logger.LogWarning(
                    "Conflict when assigning employee {EmployeeId} to meeting {MeetingId}: {Message}",
                    request.EmployeeId, meetingId, conflict.Message);
                return Result.Failure(EmployeeErrors.ScheduleConflict);
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
                        _logger.LogWarning(
                            "Transfer conflict when reassigning meeting {MeetingId}: {Message}",
                            meetingId, transferConflict.Message);
                        return Result.Failure(EmployeeErrors.ScheduleConflict);
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
                    "Employee {EmployeeId} assigned to meeting {MeetingId}",
                    request.EmployeeId, meetingId);

                return Result.Success();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex,
                    "Unique constraint violation when assigning employee {EmployeeId} to meeting {MeetingId}",
                    request.EmployeeId, meetingId);

                return Result.Failure(new Error(
                    "Meeting.SlotAlreadyTaken",
                    "The time slot was reserved by a concurrent operation. Please try again."));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error assigning employee to meeting {MeetingId}", meetingId);
            return Result.Failure(Error.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RescheduleOrderAsync(int orderId, RescheduleOrderRequest request, string? rescheduledById = null)
    {
        if (DateTime.TryParse(request.NewDate, out var earlyCheck) &&
            TimeSpan.TryParse(request.NewTime, out var earlyTimeCheck) &&
            earlyCheck.Date.Add(earlyTimeCheck) <= DateTime.UtcNow)
        {
            return Result.Failure(new Error("Reschedule.PastDate", "New date must be in the future."));
        }

        try
        {
            // Parse the new date/time from the request
            if (!DateTime.TryParse(request.NewDate, out var newDate))
            {
                return Result.Failure(new Error("Order.InvalidDate", "The provided date format is invalid."));
            }

            if (!TimeSpan.TryParse(request.NewTime, out var newTime))
            {
                return Result.Failure(new Error("Order.InvalidTime", "The provided time format is invalid."));
            }

            var newStart = newDate.Date.Add(newTime);

            // Find the order with its meetings
            var order = await _context.ServiceOrders
                .Include(o => o.Meetings)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

            if (order == null)
            {
                return Result.Failure(OrderErrors.NotFound);
            }

            // Only confirmed orders can be rescheduled
            if (order.OrderStatus != OrderStatus.Confirmed)
            {
                return Result.Failure(OrderErrors.CannotReschedule);
            }

            // Get the next scheduled meeting to reschedule
            var meet = order.Meetings
                .Where(m => m.Status == MeetStatus.Scheduled || m.Status == MeetStatus.Assigned)
                .OrderBy(m => m.ScheduledStart)
                .FirstOrDefault();

            if (meet == null)
            {
                return Result.Failure(new Error(
                    "Order.NoScheduledMeeting",
                    "No scheduled meeting found to reschedule."));
            }

            var newEnd = newStart.AddMinutes(meet.EstimatedDurationMinutes);

            // Determine the employee for conflict checking
            var employeeIdToCheck = request.NewEmployeeId ?? meet.AssignedEmployeeId;

            if (!employeeIdToCheck.HasValue)
            {
                return Result.Failure(new Error(
                    "Order.NoEmployee",
                    "The meeting has no assigned employee and no new one was provided."));
            }

            // Check for conflicts - if same employee, exclude current meeting
            var excludeMeetingId = (request.NewEmployeeId == null || request.NewEmployeeId == meet.AssignedEmployeeId)
                ? meet.Id
                : (int?)null;

            var conflict = await _schedulingService.CheckConflictsAsync(
                employeeIdToCheck.Value,
                newStart,
                newEnd,
                excludeMeetingId);

            if (conflict != null)
            {
                _logger.LogWarning(
                    "Conflict when rescheduling order {OrderId}: {Message}",
                    orderId, conflict.Message);
                return Result.Failure(new Error(
                    "Order.SchedulingConflict",
                    conflict.Message));
            }

            // Use transaction for consistency
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Release old slots
                if (meet.AssignedEmployeeId.HasValue)
                {
                    await _schedulingService.ReleaseSlotsAsync(meet.Id, OccupancyType.Meeting);
                }

                // Update the meeting
                var oldStart = meet.ScheduledStart;
                var previousStatus = meet.Status;
                meet.ScheduledStart = newStart;
                meet.ScheduledEnd = newEnd;
                meet.Status = MeetStatus.Rescheduled;

                if (request.NewEmployeeId.HasValue)
                {
                    meet.AssignedEmployeeId = request.NewEmployeeId;
                }

                // Acquire new slots
                await _schedulingService.AcquireSlotsAsync(
                    employeeIdToCheck.Value,
                    newStart,
                    newEnd,
                    OccupancyType.Meeting,
                    meet.Id);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Record audit trail
                await _statusHistoryService.RecordMeetStatusChangeAsync(
                    meet.Id,
                    previousStatus,
                    MeetStatus.Rescheduled,
                    rescheduledById,
                    notes: $"Rescheduled from {oldStart:yyyy-MM-dd HH:mm} to {newStart:yyyy-MM-dd HH:mm}");

                _logger.LogInformation(
                    "Order {OrderId} rescheduled: meeting {MeetingId} from {OldStart} to {NewStart}",
                    orderId, meet.Id, oldStart, newStart);

                return Result.Success();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex,
                    "Unique constraint violation when rescheduling order {OrderId}", orderId);

                return Result.Failure(new Error(
                    "Order.SlotAlreadyTaken",
                    "The new time slot was reserved by a concurrent operation. Please try again."));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error rescheduling order {OrderId}", orderId);
            return Result.Failure(Error.Unexpected);
        }
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
