using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record AssignEmployeeRequest
{
    [Required(ErrorMessage = "Employee ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid employee ID")]
    public int EmployeeId { get; init; }
}
