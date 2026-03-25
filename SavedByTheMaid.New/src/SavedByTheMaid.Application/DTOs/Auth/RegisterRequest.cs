using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Auth;

public record RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string Password { get; init; } = "";

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; init; }

    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? Name { get; init; }
}
