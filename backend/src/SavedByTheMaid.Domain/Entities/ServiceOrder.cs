using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Cleaning service order
/// </summary>
public class ServiceOrder : BaseAuditableEntity
{
    public string? CustomerId { get; set; }
    public virtual ApplicationUser? Customer { get; set; }

    // Location
    public int? ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }
    public required string ZipCode { get; set; }
    public required string Address { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Service type
    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public int? CleaningPlaceId { get; set; }
    public virtual CleaningPlace? CleaningPlace { get; set; }

    // Place details
    public int Bedrooms { get; set; } = 1;
    public int Bathrooms { get; set; } = 1;
    public int? SquareFootage { get; set; }
    public DirtLevel DirtLevel { get; set; } = DirtLevel.Normal;
    public bool HasPets { get; set; } = false;
    public int? FloorLevel { get; set; }
    public bool HasElevator { get; set; } = true;

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    // Statuses
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Draft;
    public OrderSource Source { get; set; } = OrderSource.Website;

    // Recurrence
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public DateTime? RecurrenceEndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public string? RecurrencePattern { get; set; } // iCal RRULE or custom JSON

    // Contact information
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? SpecialInstructions { get; set; }

    // Schedule preferences
    public TimeSpan? PreferredStartTime { get; set; }
    public TimeSpan? PreferredEndTime { get; set; }

    // Total estimated duration (calculated)
    public int EstimatedDurationMinutes { get; set; }

    // Optimistic concurrency token — incremented by the caller on every update.
    // Uses ConcurrencyCheck because MySql.EntityFrameworkCore does not support rowversion/timestamp columns.
    [System.ComponentModel.DataAnnotations.ConcurrencyCheck]
    public int Version { get; set; } = 0;

    // Navigation
    public virtual ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();
    public virtual ICollection<ServiceMeet> Meetings { get; set; } = new List<ServiceMeet>();
    public virtual ICollection<ServiceOrderRoom> Rooms { get; set; } = new List<ServiceOrderRoom>();
}

/// <summary>
/// Order detail line item (additional services, etc.)
/// </summary>
public class ServiceOrderItem : BaseEntity
{
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int? AdditionalServiceTypeId { get; set; }
    public virtual AdditionalServiceType? AdditionalServiceType { get; set; }

    public required string Description { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Rooms selected in an order
/// </summary>
public class ServiceOrderRoom : BaseEntity
{
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int CleaningPlaceRoomId { get; set; }
    public virtual CleaningPlaceRoom? CleaningPlaceRoom { get; set; }

    /// <summary>
    /// Quantity of this room type (e.g., 3 bedrooms)
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Calculated time for these rooms
    /// </summary>
    public int CalculatedMinutes { get; set; }

    /// <summary>
    /// Calculated price for these rooms
    /// </summary>
    public decimal CalculatedPrice { get; set; }
}
