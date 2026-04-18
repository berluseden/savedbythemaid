using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record OrderSummaryDto
{
    public int Id { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public string Address { get; init; } = "";
    public string? City { get; init; }
    public string ZipCode { get; init; } = "";
    public string? ServiceAreaName { get; init; }
    public string? ServiceTypeName { get; init; }
    public decimal Total { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ScheduledDate { get; init; }
}
