using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Tipo de lugar a limpiar (ej: Casa, Apartamento, Oficina)
/// </summary>
public class CleaningPlace : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navegación
    public virtual ICollection<CleaningPlaceRoom> Rooms { get; set; } = new List<CleaningPlaceRoom>();
}

/// <summary>
/// Tipo de habitación/área a limpiar (ej: Recámara, Baño, Cocina)
/// </summary>
public class CleaningPlaceRoom : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Tiempo base en minutos para limpiar esta habitación
    /// </summary>
    public int BaseMinutes { get; set; } = 15;

    /// <summary>
    /// Precio base para esta habitación
    /// </summary>
    public decimal BasePrice { get; set; } = 10.00m;
    
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    
    public int CleaningPlaceId { get; set; }
    public virtual CleaningPlace? CleaningPlace { get; set; }

    // Navegación
    public virtual ICollection<RoomServiceType> ServiceTypes { get; set; } = new List<RoomServiceType>();
}
