using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error NotFound = new(
        "Order.NotFound",
        "The order was not found.");

    public static readonly Error AlreadyCancelled = new(
        "Order.AlreadyCancelled",
        "The order has already been cancelled.");

    public static readonly Error CannotReschedule = new(
        "Order.CannotReschedule",
        "Only confirmed orders can be rescheduled.");

    public static Error InvalidStatusTransition(OrderStatus from, OrderStatus to) => new(
        "Order.InvalidStatusTransition",
        $"Invalid status transition from {from} to {to}.");

    public static Error NotOwnedByCustomer(int orderId, string customerId) => new(
        "Order.NotOwnedByCustomer",
        $"Order {orderId} does not belong to customer {customerId}.");

    public static Error CannotCancelInStatus(OrderStatus currentStatus) => new(
        "Order.CannotCancelInStatus",
        $"Orders in status {currentStatus} cannot be cancelled.");

    public static Error MeetingNotFound(int meetingId) => new(
        "Order.MeetingNotFound",
        $"Meeting with ID {meetingId} was not found.");
}
