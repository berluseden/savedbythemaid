using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record CancelOrderRequest
{
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; init; }
}
