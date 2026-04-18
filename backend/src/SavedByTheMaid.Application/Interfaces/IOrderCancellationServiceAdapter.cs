namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for order cancellation.
/// Implemented by the Api layer's OrderCancellationService via a thin adapter or directly.
/// </summary>
public interface IOrderCancellationServiceAdapter
{
    /// <summary>
    /// Cancels an order and all its pending meetings, releasing occupied slots.
    /// </summary>
    /// <returns>Tuple of (success, errorMessage). ErrorMessage is null on success.</returns>
    Task<(bool Success, string? Error)> CancelOrderAsync(int orderId, string? reason, string? cancelledById);
}
