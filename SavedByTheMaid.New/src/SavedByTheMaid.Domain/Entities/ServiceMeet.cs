using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Cita de servicio de limpieza (una orden puede tener múltiples citas si es recurrente)
/// </summary>
public class ServiceMeet : BaseAuditableEntity
{
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int? AssignedEmployeeId { get; set; }
    public virtual Employee? AssignedEmployee { get; set; }

    // Zona de servicio (denormalizado para reportes)
    public int? ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    // Horarios planificados
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    
    // Horarios reales
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }

    public MeetStatus Status { get; set; } = MeetStatus.Scheduled;

    /// <summary>
    /// Duración estimada en minutos
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    // Geolocalización Check-In
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public DateTime? CheckInTime { get; set; }

    // Geolocalización Check-Out
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }

    // Ajustes por incidencias
    public AdjustmentStatus AdjustmentStatus { get; set; } = AdjustmentStatus.None;
    public decimal? AdjustmentAmount { get; set; }
    public string? AdjustmentReason { get; set; }

    // Notas y fotos
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? PhotosJson { get; set; } // JSON array de URLs

    // Rating del cliente
    public int? CustomerRating { get; set; } // 1-5
    public string? CustomerFeedback { get; set; }
}
