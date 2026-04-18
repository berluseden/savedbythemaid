using System.ComponentModel.DataAnnotations;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Booking;

public record ConfirmBookingRequest
{
    // Reservation
    [Required(ErrorMessage = "Reservation is required")]
    [Range(1, int.MaxValue)]
    public int SoftReserveId { get; init; }

    [Required(ErrorMessage = "Session ID is required")]
    [StringLength(100, MinimumLength = 10)]
    public string SessionId { get; init; } = "";

    [StringLength(100)]
    public string? CustomerId { get; init; }

    public bool PaymentConfirmed { get; init; }

    // Address
    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters")]
    public string Address { get; init; } = "";

    [StringLength(200)]
    public string? AddressLine2 { get; init; }

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(50)]
    public string? State { get; init; }

    // Service
    [Required(ErrorMessage = "Service type is required")]
    [Range(1, int.MaxValue)]
    public int ServiceTypeId { get; init; }

    public int? CleaningPlaceId { get; init; }

    [Range(0, 20, ErrorMessage = "Invalid number of bedrooms")]
    public int Bedrooms { get; init; } = 1;

    [Range(0, 20, ErrorMessage = "Invalid number of bathrooms")]
    public int Bathrooms { get; init; } = 1;

    [Range(0, 50000)]
    public int? SquareFootage { get; init; }

    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;
    public bool HasPets { get; init; }

    [Range(0, 100)]
    public int? FloorLevel { get; init; }

    public bool HasElevator { get; init; } = true;
    public bool IsFirstTime { get; init; }
    public List<int>? AdditionalServiceIds { get; init; }
    public List<RoomSelectionDto>? Rooms { get; init; }

    // Amounts
    [Range(0, 100000)]
    public decimal Subtotal { get; init; }

    [Range(0, 100000)]
    public decimal Tax { get; init; }

    [Range(0, 100000)]
    public decimal Discount { get; init; }

    [Range(0, 100000)]
    public decimal Total { get; init; }

    // Recurrence
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;
    public DateTime? RecurrenceEndDate { get; init; }

    // Contact
    [StringLength(100)]
    public string? ContactName { get; init; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20)]
    public string? ContactPhone { get; init; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256)]
    public string ContactEmail { get; init; } = "";

    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; init; }

    [StringLength(1000)]
    public string? SpecialInstructions { get; init; }
}
