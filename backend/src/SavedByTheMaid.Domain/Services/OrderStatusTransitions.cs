using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Errors;

namespace SavedByTheMaid.Domain.Services;

/// <summary>
/// Defines valid order status transitions using the Result pattern.
/// Replaces the inline dictionary that was recreated per request in AdminOrdersController.
/// </summary>
public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [OrderStatus.PendingReview] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        #pragma warning disable CS0618
        [OrderStatus.Draft] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        #pragma warning restore CS0618
        [OrderStatus.Confirmed] = [OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow],
        [OrderStatus.InProgress] = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.NoShow] = []
    };

    /// <summary>
    /// Terminal states from which no further transitions are allowed.
    /// </summary>
    public static readonly OrderStatus[] FinalStates =
    [
        OrderStatus.Completed,
        OrderStatus.Cancelled,
        OrderStatus.NoShow
    ];

    /// <summary>
    /// Active states indicating the order is still in progress.
    /// </summary>
    public static readonly OrderStatus[] ActiveStates =
    [
        OrderStatus.PendingReview,
        OrderStatus.Confirmed,
        OrderStatus.InProgress
    ];

    /// <summary>
    /// Checks whether a transition from one status to another is valid.
    /// </summary>
    public static bool IsValid(OrderStatus from, OrderStatus to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>
    /// Validates a transition and returns a Result indicating success or failure.
    /// </summary>
    public static Result Validate(OrderStatus from, OrderStatus to)
        => IsValid(from, to)
            ? Result.Success()
            : Result.Failure(OrderErrors.InvalidStatusTransition(from, to));

    /// <summary>
    /// Gets all statuses that can be transitioned to from the given status.
    /// </summary>
    public static OrderStatus[] GetAllowedTransitions(OrderStatus current)
        => ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : [];

    /// <summary>
    /// Returns true if the status is a terminal (final) state.
    /// </summary>
    public static bool IsFinalState(OrderStatus status)
        => FinalStates.Contains(status);

    /// <summary>
    /// Returns true if the status is an active (non-terminal) state.
    /// </summary>
    public static bool IsActiveState(OrderStatus status)
        => ActiveStates.Contains(status);
}
