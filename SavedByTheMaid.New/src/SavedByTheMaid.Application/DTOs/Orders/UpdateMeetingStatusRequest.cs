using System.ComponentModel.DataAnnotations;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Orders;

public record UpdateMeetingStatusRequest
{
    [Required(ErrorMessage = "Meeting status is required")]
    public MeetStatus Status { get; init; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; init; }
}
