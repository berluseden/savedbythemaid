using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Reserva temporal para evitar colisiones durante el checkout
/// Se convierte en reserva confirmada al completar el pago
/// </summary>
public class SoftReserve : BaseEntity
{
    /// <summary>
    /// ID de sesión del navegador para vincular reservas anónimas
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Usuario que hizo la reserva (opcional si es anónimo)
    /// </summary>
    public string? CustomerId { get; set; }
    public virtual ApplicationUser? Customer { get; set; }

    /// <summary>
    /// Empleada reservada
    /// </summary>
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Zona de servicio
    /// </summary>
    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    /// <summary>
    /// Horario reservado
    /// </summary>
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }

    /// <summary>
    /// Cuándo expira esta reserva temporal (típicamente 15-30 minutos)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public SoftReserveStatus Status { get; set; } = SoftReserveStatus.Active;

    /// <summary>
    /// Si la reserva fue confirmada, referencia a la orden
    /// </summary>
    public int? ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }
}
