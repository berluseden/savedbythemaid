using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Tiempo libre, vacaciones, permisos o bloqueos manuales de empleadas
/// </summary>
public class EmployeeTimeOff : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Fecha y hora de inicio del bloqueo
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Fecha y hora de fin del bloqueo
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Si es día completo (ignora horas)
    /// </summary>
    public bool IsAllDay { get; set; } = false;

    public TimeOffType Type { get; set; } = TimeOffType.TimeOff;

    public string? Reason { get; set; }

    /// <summary>
    /// Estado de aprobación (si aplica flujo de aprobación)
    /// </summary>
    public TimeOffStatus Status { get; set; } = TimeOffStatus.Approved;
}
