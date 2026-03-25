using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for recording status change audit history.
/// Implemented by the Api layer's StatusHistoryService via a thin adapter or directly.
/// </summary>
public interface IStatusHistoryServiceAdapter
{
    /// <summary>
    /// Records an order status change in the audit history.
    /// </summary>
    Task RecordOrderStatusChangeAsync(
        int serviceOrderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null);

    /// <summary>
    /// Records a meeting status change in the audit history.
    /// </summary>
    Task RecordMeetStatusChangeAsync(
        int serviceMeetId,
        MeetStatus? fromStatus,
        MeetStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null);
}
