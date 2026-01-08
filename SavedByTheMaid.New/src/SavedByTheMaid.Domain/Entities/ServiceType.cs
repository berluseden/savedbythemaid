using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Tipo de servicio de limpieza (ej: Limpieza Profunda, Regular, etc.)
/// </summary>
public class ServiceType : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Costo operativo del servicio (para cálculos internos)
    /// </summary>
    public decimal Cost { get; set; }
    
    /// <summary>
    /// Precio base (incluye 1 recámara + 1 baño)
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Precio adicional por cada recámara extra
    /// </summary>
    public decimal PricePerBedroom { get; set; } = 15.00m;
    
    /// <summary>
    /// Precio adicional por cada baño extra
    /// </summary>
    public decimal PricePerBathroom { get; set; } = 20.00m;
    
    /// <summary>
    /// Duración estimada en minutos (base)
    /// </summary>
    public int EstimatedMinutes { get; set; } = 60;
    
    /// <summary>
    /// Minutos adicionales por recámara
    /// </summary>
    public int MinutesPerBedroom { get; set; } = 20;
    
    /// <summary>
    /// Minutos adicionales por baño
    /// </summary>
    public int MinutesPerBathroom { get; set; } = 15;
    
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
    
    /// <summary>
    /// Precio del servicio adicional
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Tiempo adicional en minutos
    /// </summary>
    public int AdditionalMinutes { get; set; } = 30;
    
    public bool IsActive { get; set; } = true;
}
