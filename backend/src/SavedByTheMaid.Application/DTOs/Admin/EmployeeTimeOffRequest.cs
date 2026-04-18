using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.DTOs.Admin;

public record CreateEmployeeTimeOffRequest(
    DateTime StartDateTime,
    DateTime EndDateTime,
    bool IsAllDay,
    TimeOffType Type,
    string? Reason
);
