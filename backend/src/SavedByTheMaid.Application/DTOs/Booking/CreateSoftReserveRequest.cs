using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Booking;

public record CreateSoftReserveRequest
{
    [StringLength(100)]
    public string? SessionId { get; init; }

    [StringLength(100)]
    public string? CustomerId { get; init; }

    [Required(ErrorMessage = "Employee is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid employee ID")]
    public int EmployeeId { get; init; }

    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; init; }

    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; init; }

    [Required(ErrorMessage = "Estimated duration is required")]
    [Range(30, 480, ErrorMessage = "Duration must be between 30 and 480 minutes")]
    public int EstimatedMinutes { get; init; }
}
