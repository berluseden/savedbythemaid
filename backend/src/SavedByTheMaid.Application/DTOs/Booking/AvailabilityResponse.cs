namespace SavedByTheMaid.Application.DTOs.Booking;

public record AvailabilityResponse
{
    public DateTime Date { get; init; }
    public string ZipCode { get; init; } = "";
    public int ServiceAreaId { get; init; }
    public List<TimeSlotDto> Slots { get; init; } = [];
    public int TotalSlotsAvailable { get; init; }
}

public record TimeSlotDto
{
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string FormattedTime { get; init; } = "";
    public List<int> AvailableEmployeeIds { get; init; } = [];
    public bool IsAvailable { get; init; }
}
