using System.ComponentModel.DataAnnotations;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "New order status is required")]
    public OrderStatus OrderStatus { get; init; }
}
