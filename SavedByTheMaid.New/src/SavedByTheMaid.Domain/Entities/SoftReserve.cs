using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Temporary reservation to avoid collisions during checkout.
/// Becomes a confirmed reservation upon payment completion.
/// </summary>
public class SoftReserve : BaseEntity
{
    /// <summary>
    /// Browser session ID to link anonymous reservations
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// User who made the reservation (optional if anonymous)
    /// </summary>
    public string? CustomerId { get; set; }
    public virtual ApplicationUser? Customer { get; set; }

    /// <summary>
    /// Reserved employee
    /// </summary>
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Service area
    /// </summary>
    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    /// <summary>
    /// Reserved time slot
    /// </summary>
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }

    /// <summary>
    /// When this temporary reservation expires (typically 15-30 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public SoftReserveStatus Status { get; set; } = SoftReserveStatus.Active;

    /// <summary>
    /// Number of times this reservation has been extended (max 2)
    /// </summary>
    public int ExtensionCount { get; set; } = 0;

    /// <summary>
    /// If the reservation was confirmed, reference to the order
    /// </summary>
    public int? ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }
}
