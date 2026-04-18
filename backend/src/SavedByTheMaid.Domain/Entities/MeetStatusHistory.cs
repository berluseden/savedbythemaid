using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Service appointment status change history (audit)
/// </summary>
public class MeetStatusHistory : BaseEntity
{
    /// <summary>
    /// Service appointment ID
    /// </summary>
    public int ServiceMeetId { get; set; }
    public virtual ServiceMeet? ServiceMeet { get; set; }

    /// <summary>
    /// Previous status (can be null if it is the initial creation)
    /// </summary>
    public MeetStatus? FromStatus { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public MeetStatus ToStatus { get; set; }

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
    /// Reason code for the change (e.g., "EMPLOYEE_CHECKIN", "ADMIN_CANCEL", "SYSTEM_RESCHEDULE")
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Additional notes about the change
    /// </summary>
    public string? Notes { get; set; }
}
