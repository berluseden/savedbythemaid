using SavedByTheMaid.Application.DTOs.Orders;
using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application service for order and meeting management operations.
/// Used by both admin and customer-facing controllers.
/// </summary>
public interface IOrderManagementService
{
    /// <summary>
    /// Cancels an order and all its pending meetings, releasing occupied slots.
    /// </summary>
    Task<Result> CancelOrderAsync(int orderId, CancelOrderRequest request, string? cancelledById = null);

    /// <summary>
    /// Reschedules an order to a new date and time.
    /// Only confirmed orders can be rescheduled.
    /// </summary>
    Task<Result> RescheduleOrderAsync(int orderId, RescheduleOrderRequest request, string? rescheduledById = null);

    /// <summary>
    /// Updates the status of an order with transition validation.
    /// </summary>
    Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, string? changedById = null);

    /// <summary>
    /// Updates the status of a meeting (appointment) with transition validation.
    /// </summary>
    Task<Result> UpdateMeetingStatusAsync(int meetingId, UpdateMeetingStatusRequest request, string? changedById = null);

    /// <summary>
    /// Assigns an employee to a meeting, validating schedule and conflicts.
    /// If an employee was previously assigned, transfers the slots.
    /// </summary>
    Task<Result> AssignEmployeeAsync(int meetingId, AssignEmployeeRequest request);
}
