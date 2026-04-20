using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Slot occupancy entity that implements the anti-collision model.
/// The UNIQUE constraint on (EmployeeId, SlotStart) guarantees no double-booking
/// at the database level, eliminating race conditions.
/// </summary>
public class SlotOccupancy : BaseEntity
{
    /// <summary>
    /// ID of the employee who occupies this slot (null = unassigned capacity slot)
    /// </summary>
    public int? EmployeeId { get; set; }
    
    /// <summary>
    /// Start of the occupied slot (recommended granularity: 30 min)
    /// </summary>
    public DateTime SlotStart { get; set; }
    
    /// <summary>
    /// End of the occupied slot
    /// </summary>
    public DateTime SlotEnd { get; set; }
    
    /// <summary>
    /// Occupancy type: SoftReserve (temporary) or Meeting (confirmed)
    /// </summary>
    public OccupancyType OccupancyType { get; set; }
    
    /// <summary>
    /// Reference ID to the SoftReserve or ServiceMeet depending on OccupancyType
    /// </summary>
    public int ReferenceId { get; set; }
    
    /// <summary>
    /// Expiration date (only applies to SoftReserve, null for Meeting)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}
