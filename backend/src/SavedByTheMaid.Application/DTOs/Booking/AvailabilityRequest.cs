using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Booking;

public record AvailabilityRequest
{
    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "ZIP code must be between 5 and 10 characters")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    public string ZipCode { get; init; } = "";

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; init; }

    [Required(ErrorMessage = "Estimated duration is required")]
    [Range(30, 480, ErrorMessage = "Duration must be between 30 and 480 minutes")]
    public int EstimatedMinutes { get; init; }

    public List<int>? RequiredEquipmentIds { get; init; }
}
