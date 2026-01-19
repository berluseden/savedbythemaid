using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Historial de cambios de estado de órdenes de servicio (auditoría)
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    /// <summary>
    /// ID de la orden de servicio
    /// </summary>
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    /// <summary>
    /// Estado anterior (puede ser null si es la creación inicial)
    /// </summary>
    public OrderStatus? FromStatus { get; set; }

    /// <summary>
    /// Estado nuevo
    /// </summary>
    public OrderStatus ToStatus { get; set; }

    /// <summary>
    /// ID del usuario que realizó el cambio (null = sistema/automático)
    /// </summary>
    public string? ChangedById { get; set; }
    public virtual ApplicationUser? ChangedBy { get; set; }

    /// <summary>
    /// Fecha y hora del cambio
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Código de razón del cambio (ej: "ADMIN_APPROVAL", "CUSTOMER_CANCEL", "SYSTEM_EXPIRE")
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Notas adicionales sobre el cambio
    /// </summary>
    public string? Notes { get; set; }
}
