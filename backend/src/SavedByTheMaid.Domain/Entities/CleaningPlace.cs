using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Type of place to clean (e.g., House, Apartment, Office)
/// </summary>
public class CleaningPlace : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<CleaningPlaceRoom> Rooms { get; set; } = new List<CleaningPlaceRoom>();
}

/// <summary>
/// Type of room/area to clean (e.g., Bedroom, Bathroom, Kitchen)
/// </summary>
public class CleaningPlaceRoom : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Base time in minutes to clean this room
    /// </summary>
    public int BaseMinutes { get; set; } = 15;

    /// <summary>
    /// Base price for this room
    /// </summary>
    public decimal BasePrice { get; set; } = 10.00m;
    
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    
    public int CleaningPlaceId { get; set; }
    public virtual CleaningPlace? CleaningPlace { get; set; }

    // Navigation
    public virtual ICollection<RoomServiceType> ServiceTypes { get; set; } = new List<RoomServiceType>();
}
