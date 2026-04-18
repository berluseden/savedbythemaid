namespace SavedByTheMaid.Application.DTOs.Admin;

public record CreateEmployeeScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes = 30,
    bool IsAvailable = true
);
