using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Cleaning service appointment (an order can have multiple appointments if it is recurring)
/// </summary>
public class ServiceMeet : BaseAuditableEntity
{
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int? AssignedEmployeeId { get; set; }
    public virtual Employee? AssignedEmployee { get; set; }

    // Service area (denormalized for reports)
    public int? ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    // Scheduled times
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    
    // Actual times
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }

    public MeetStatus Status { get; set; } = MeetStatus.Scheduled;

    /// <summary>
    /// Estimated duration in minutes
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    // Geolocation Check-In
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public DateTime? CheckInTime { get; set; }

    // Geolocation Check-Out
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public DateTime? CheckOutTime { get; set; }

    // Incident adjustments
    public AdjustmentStatus AdjustmentStatus { get; set; } = AdjustmentStatus.None;
    public decimal? AdjustmentAmount { get; set; }
    public string? AdjustmentReason { get; set; }

    // Notes and photos
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? PhotosJson { get; set; } // JSON array of URLs

    // Customer rating
    public int? CustomerRating { get; set; } // 1-5
    public string? CustomerFeedback { get; set; }
}
