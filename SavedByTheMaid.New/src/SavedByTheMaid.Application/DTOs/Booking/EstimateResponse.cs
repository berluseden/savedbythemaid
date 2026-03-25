using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Booking;

public record EstimateResponse
{
    public int EstimatedMinutes { get; init; }
    public string FormattedDuration { get; init; } = "";
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public decimal DiscountPercent { get; init; }
}
