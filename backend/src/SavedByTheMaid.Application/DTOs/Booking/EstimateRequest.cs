using System.ComponentModel.DataAnnotations;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Booking;

public record EstimateRequest
{
    [Required(ErrorMessage = "Service type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid service type ID")]
    public int ServiceTypeId { get; init; }

    public int? CleaningPlaceId { get; init; }

    public List<RoomSelectionDto> Rooms { get; init; } = [];

    public List<int> AdditionalServiceIds { get; init; } = [];

    [Range(1, 20, ErrorMessage = "Bedrooms must be between 1 and 20")]
    public int Bedrooms { get; init; } = 1;

    [Range(1, 20, ErrorMessage = "Bathrooms must be between 1 and 20")]
    public int Bathrooms { get; init; } = 1;

    [Range(0, 50000, ErrorMessage = "Square footage must be between 0 and 50,000")]
    public int? SquareFootage { get; init; }

    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;

    public bool HasPets { get; init; }

    public bool HasElevator { get; init; } = true;

    public bool IsFirstTime { get; init; } = true;
}

public record RoomSelectionDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid room ID")]
    public int RoomId { get; init; }

    [Range(1, 20, ErrorMessage = "Quantity must be between 1 and 20")]
    public int Quantity { get; init; } = 1;
}
