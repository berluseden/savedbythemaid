using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Relationship between rooms and allowed service types
/// Defines which services are valid for each space
/// </summary>
public class RoomServiceType : BaseEntity
{
    public int CleaningPlaceRoomId { get; set; }
    public virtual CleaningPlaceRoom? CleaningPlaceRoom { get; set; }

    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    /// <summary>
    /// Base time in minutes for this service in this room
    /// Overrides the room's base time if defined
    /// </summary>
    public int? BaseMinutesOverride { get; set; }

    /// <summary>
    /// Base price for this service in this room
    /// Overrides the service's base price if defined
    /// </summary>
    public decimal? BasePriceOverride { get; set; }

    public bool IsActive { get; set; } = true;
}
