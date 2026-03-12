using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Type of cleaning service (e.g., Deep Cleaning, Regular, etc.)
/// </summary>
public class ServiceType : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Operational cost of the service (for internal calculations)
    /// </summary>
    public decimal Cost { get; set; }
    
    /// <summary>
    /// Base price (includes 1 bedroom + 1 bathroom)
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Additional price per extra bedroom
    /// </summary>
    public decimal PricePerBedroom { get; set; } = 15.00m;
    
    /// <summary>
    /// Additional price per extra bathroom
    /// </summary>
    public decimal PricePerBathroom { get; set; } = 20.00m;
    
    /// <summary>
    /// Estimated duration in minutes (base)
    /// </summary>
    public int EstimatedMinutes { get; set; } = 60;
    
    /// <summary>
    /// Additional minutes per bedroom
    /// </summary>
    public int MinutesPerBedroom { get; set; } = 20;
    
    /// <summary>
    /// Additional minutes per bathroom
    /// </summary>
    public int MinutesPerBathroom { get; set; } = 15;
    
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public virtual ICollection<ServiceTypeEquipment> RequiredEquipment { get; set; } = new List<ServiceTypeEquipment>();
    public virtual ICollection<RoomServiceType> RoomServiceTypes { get; set; } = new List<RoomServiceType>();
    public virtual ICollection<PriceMultiplier> PriceMultipliers { get; set; } = new List<PriceMultiplier>();
}

/// <summary>
/// Additional service (e.g., Oven cleaning, Windows, etc.)
/// </summary>
public class AdditionalServiceType : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Price of the additional service
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Additional time in minutes
    /// </summary>
    public int AdditionalMinutes { get; set; } = 30;
    
    public bool IsActive { get; set; } = true;
}
