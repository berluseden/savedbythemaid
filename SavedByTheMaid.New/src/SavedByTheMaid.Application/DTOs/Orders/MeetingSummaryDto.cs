using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record MeetingSummaryDto
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public DateTime? ActualStart { get; init; }
    public DateTime? ActualEnd { get; init; }
    public int? EmployeeId { get; init; }
    public string? EmployeeName { get; init; }
    public string? ServiceAreaName { get; init; }
    public string? Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public MeetStatus Status { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public AdjustmentStatus AdjustmentStatus { get; init; }
    public decimal? AdjustmentAmount { get; init; }
}
