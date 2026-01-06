using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Tipo de servicio de limpieza (ej: Limpieza Profunda, Regular, etc.)
/// </summary>
public class ServiceType : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    
    /// <summary>
    /// Duración estimada en minutos
    /// </summary>
    public int EstimatedMinutes { get; set; } = 60;
    
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navegación
    public virtual ICollection<ServiceTypeEquipment> RequiredEquipment { get; set; } = new List<ServiceTypeEquipment>();
    public virtual ICollection<RoomServiceType> RoomServiceTypes { get; set; } = new List<RoomServiceType>();
    public virtual ICollection<PriceMultiplier> PriceMultipliers { get; set; } = new List<PriceMultiplier>();
}

/// <summary>
/// Servicio adicional (ej: Limpieza de horno, ventanas, etc.)
/// </summary>
public class AdditionalServiceType : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    
    /// <summary>
    /// Tiempo adicional en minutos
    /// </summary>
    public int AdditionalMinutes { get; set; } = 30;
    
    public bool IsActive { get; set; } = true;
}
