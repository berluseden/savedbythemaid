using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Admin;

public record CreateEmployeeRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
    public string FirstName { get; init; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
    public string LastName { get; init; } = "";

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string? Email { get; init; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; init; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid service area ID")]
    public int? PrimaryServiceAreaId { get; init; }

    [Range(1, 24, ErrorMessage = "Max daily hours must be between 1 and 24")]
    public int? MaxDailyHours { get; init; } = 8;

    [Range(1, 20, ErrorMessage = "Max daily services must be between 1 and 20")]
    public int? MaxDailyServices { get; init; } = 4;
}
