using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record RescheduleOrderRequest
{
    [Required(ErrorMessage = "New date is required")]
    [StringLength(10, ErrorMessage = "Date format should be yyyy-MM-dd")]
    public string NewDate { get; init; } = "";

    [Required(ErrorMessage = "New time is required")]
    [StringLength(8, ErrorMessage = "Time format should be HH:mm or HH:mm:ss")]
    public string NewTime { get; init; } = "";

    public int? NewEmployeeId { get; init; }
}
