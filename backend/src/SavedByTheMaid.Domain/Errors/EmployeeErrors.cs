using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Errors;

public static class EmployeeErrors
{
    public static readonly Error NotFound = new(
        "Employee.NotFound",
        "The employee was not found.");

    public static readonly Error Inactive = new(
        "Employee.Inactive",
        "The employee is inactive and cannot be assigned.");

    public static readonly Error AreaMismatch = new(
        "Employee.AreaMismatch",
        "The employee does not work in the requested service area.");

    public static readonly Error ScheduleConflict = new(
        "Employee.ScheduleConflict",
        "The employee has a scheduling conflict at the requested time.");

    public static readonly Error DailyLimitReached = new(
        "Employee.DailyLimitReached",
        "The employee has reached the maximum number of daily services.");

    public static Error NotAvailableOnDay(int employeeId, DayOfWeek day) => new(
        "Employee.NotAvailableOnDay",
        $"Employee {employeeId} does not work on {day}.");

    public static Error OutsideWorkingHours(int employeeId) => new(
        "Employee.OutsideWorkingHours",
        $"The appointment is outside employee {employeeId}'s working hours.");

    public static Error ConflictWithExisting(int employeeId, DateTime conflictStart, DateTime conflictEnd) => new(
        "Employee.ConflictWithExisting",
        $"Employee {employeeId} already has a booking from {conflictStart:HH:mm} to {conflictEnd:HH:mm}.");

    public static Error MaxDailyHoursExceeded(int employeeId, int maxHours) => new(
        "Employee.MaxDailyHoursExceeded",
        $"Employee {employeeId} would exceed the maximum of {maxHours} daily hours.");
}
