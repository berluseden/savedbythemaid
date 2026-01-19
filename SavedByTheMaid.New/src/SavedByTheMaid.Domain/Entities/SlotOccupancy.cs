using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Entidad de ocupación de slot que implementa el modelo de anti-colisión.
/// El constraint UNIQUE en (EmployeeId, SlotStart) garantiza que no haya double-booking
/// a nivel de base de datos, eliminando race conditions.
/// </summary>
public class SlotOccupancy : BaseEntity
{
    /// <summary>
    /// ID de la empleada que tiene ocupado este slot
    /// </summary>
    public int EmployeeId { get; set; }
    
    /// <summary>
    /// Inicio del slot ocupado (granularidad recomendada: 30 min)
    /// </summary>
    public DateTime SlotStart { get; set; }
    
    /// <summary>
    /// Fin del slot ocupado
    /// </summary>
    public DateTime SlotEnd { get; set; }
    
    /// <summary>
    /// Tipo de ocupación: SoftReserve (temporal) o Meeting (confirmado)
    /// </summary>
    public OccupancyType OccupancyType { get; set; }
    
    /// <summary>
    /// ID de referencia al SoftReserve o ServiceMeet según OccupancyType
    /// </summary>
    public int ReferenceId { get; set; }
    
    /// <summary>
    /// Fecha de expiración (solo aplica para SoftReserve, null para Meeting)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navegación
    public virtual Employee? Employee { get; set; }
}
