using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Historial de cambios de estado de citas de servicio (auditoría)
/// </summary>
public class MeetStatusHistory : BaseEntity
{
    /// <summary>
    /// ID de la cita de servicio
    /// </summary>
    public int ServiceMeetId { get; set; }
    public virtual ServiceMeet? ServiceMeet { get; set; }

    /// <summary>
    /// Estado anterior (puede ser null si es la creación inicial)
    /// </summary>
    public MeetStatus? FromStatus { get; set; }

    /// <summary>
    /// Estado nuevo
    /// </summary>
    public MeetStatus ToStatus { get; set; }

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
    /// Código de razón del cambio (ej: "EMPLOYEE_CHECKIN", "ADMIN_CANCEL", "SYSTEM_RESCHEDULE")
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Notas adicionales sobre el cambio
    /// </summary>
    public string? Notes { get; set; }
}
