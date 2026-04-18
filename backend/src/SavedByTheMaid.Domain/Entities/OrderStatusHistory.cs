using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Service order status change history (audit)
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    /// <summary>
    /// Service order ID
    /// </summary>
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    /// <summary>
    /// Previous status (can be null if it is the initial creation)
    /// </summary>
    public OrderStatus? FromStatus { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public OrderStatus ToStatus { get; set; }

    /// <summary>
    /// ID of the user who made the change (null = system/automatic)
    /// </summary>
    public string? ChangedById { get; set; }
    public virtual ApplicationUser? ChangedBy { get; set; }

    /// <summary>
    /// Date and time of the change
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Change reason code (e.g., "ADMIN_APPROVAL", "CUSTOMER_CANCEL", "SYSTEM_EXPIRE")
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Additional notes about the change
    /// </summary>
    public string? Notes { get; set; }
}
