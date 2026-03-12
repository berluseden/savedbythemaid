using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Time off, vacations, leaves, or manual blocks for employees
/// </summary>
public class EmployeeTimeOff : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Start date and time of the block
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// End date and time of the block
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Whether it is a full day (ignores hours)
    /// </summary>
    public bool IsAllDay { get; set; } = false;

    public TimeOffType Type { get; set; } = TimeOffType.TimeOff;

    public string? Reason { get; set; }

    /// <summary>
    /// Approval status (if approval workflow applies)
    /// </summary>
    public TimeOffStatus Status { get; set; } = TimeOffStatus.Approved;
}
