namespace SavedByTheMaid.Application.DTOs.Booking;

public record SoftReserveResponse
{
    public int SoftReserveId { get; init; }
    public string SessionId { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public DateTime ExpiresAt { get; init; }
    public int TtlSeconds { get; init; }
    public string Message { get; init; } = "";
}
