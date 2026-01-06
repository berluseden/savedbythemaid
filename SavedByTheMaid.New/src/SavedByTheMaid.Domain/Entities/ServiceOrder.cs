using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Orden de servicio de limpieza
/// </summary>
public class ServiceOrder : BaseAuditableEntity
{
    public string? CustomerId { get; set; }
    public virtual ApplicationUser? Customer { get; set; }

    // Ubicación
    public int? ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }
    public required string ZipCode { get; set; }
    public required string Address { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Tipo de servicio
    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public int? CleaningPlaceId { get; set; }
    public virtual CleaningPlace? CleaningPlace { get; set; }

    // Detalles del lugar
    public int Bedrooms { get; set; } = 1;
    public int Bathrooms { get; set; } = 1;
    public int? SquareFootage { get; set; }
    public DirtLevel DirtLevel { get; set; } = DirtLevel.Normal;
    public bool HasPets { get; set; } = false;
    public int? FloorLevel { get; set; }
    public bool HasElevator { get; set; } = true;

    // Montos
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    // Estados
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Draft;
    public OrderSource Source { get; set; } = OrderSource.Website;

    // Recurrencia
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public DateTime? RecurrenceEndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public string? RecurrencePattern { get; set; } // iCal RRULE o custom JSON

    // Información de contacto
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? SpecialInstructions { get; set; }

    // Preferencias de horario
    public TimeSpan? PreferredStartTime { get; set; }
    public TimeSpan? PreferredEndTime { get; set; }

    // Duración estimada total (calculada)
    public int EstimatedDurationMinutes { get; set; }

    // Navegación
    public virtual ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();
    public virtual ICollection<ServiceMeet> Meetings { get; set; } = new List<ServiceMeet>();
    public virtual ICollection<ServiceOrderRoom> Rooms { get; set; } = new List<ServiceOrderRoom>();
}

/// <summary>
/// Línea de detalle de una orden (servicios adicionales, etc.)
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
/// Habitaciones seleccionadas en una orden
/// </summary>
public class ServiceOrderRoom : BaseEntity
{
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int CleaningPlaceRoomId { get; set; }
    public virtual CleaningPlaceRoom? CleaningPlaceRoom { get; set; }

    /// <summary>
    /// Cantidad de este tipo de habitación (ej: 3 recámaras)
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Tiempo calculado para estas habitaciones
    /// </summary>
    public int CalculatedMinutes { get; set; }

    /// <summary>
    /// Precio calculado para estas habitaciones
    /// </summary>
    public decimal CalculatedPrice { get; set; }
}
