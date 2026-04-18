namespace SavedByTheMaid.Application.DTOs.Booking;

public record CoverageResponse
{
    public bool IsCovered { get; init; }
    public int? ServiceAreaId { get; init; }
    public string? ServiceAreaName { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? County { get; init; }
    public string Message { get; init; } = "";
}
