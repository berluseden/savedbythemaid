using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Relación entre habitaciones y tipos de servicio permitidos
/// Define qué servicios son válidos en cada espacio
/// </summary>
public class RoomServiceType : BaseEntity
{
    public int CleaningPlaceRoomId { get; set; }
    public virtual CleaningPlaceRoom? CleaningPlaceRoom { get; set; }

    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    /// <summary>
    /// Tiempo base en minutos para este servicio en esta habitación
    /// Sobrescribe el tiempo base de la habitación si está definido
    /// </summary>
    public int? BaseMinutesOverride { get; set; }

    /// <summary>
    /// Precio base para este servicio en esta habitación
    /// Sobrescribe el precio base del servicio si está definido
    /// </summary>
    public decimal? BasePriceOverride { get; set; }

    public bool IsActive { get; set; } = true;
}
