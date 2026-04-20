using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for the scheduling/conflict-check service.
/// Implemented by the Api layer's SchedulingService via a thin adapter or directly.
/// </summary>
public interface ISchedulingServiceAdapter
{
    /// <summary>
    /// Checks whether a conflict exists for an employee in a given time range.
    /// </summary>
    /// <returns>A conflict result if there is one, null otherwise.</returns>
    Task<SchedulingConflictResult?> CheckConflictsAsync(int employeeId, DateTime start, DateTime end, int? excludeMeetingId = null);

    /// <summary>
    /// Acquires slots in SlotOccupancy for an employee in a given time range.
    /// </summary>
    Task AcquireSlotsAsync(int? employeeId, DateTime start, DateTime end, OccupancyType type, int referenceId, DateTime? expiresAt = null);

    /// <summary>
    /// Releases SlotOccupancy slots by referenceId and type.
    /// </summary>
    Task ReleaseSlotsAsync(int referenceId, OccupancyType type);

    /// <summary>
    /// Transfers slots from one employee to another (for reassignment).
    /// </summary>
    /// <returns>A conflict result if transfer would cause double-booking, null on success.</returns>
    Task<SchedulingConflictResult?> TransferSlotsAsync(int referenceId, OccupancyType type, int newEmployeeId);
}

/// <summary>
/// Application-layer model for scheduling conflict information.
/// </summary>
public record SchedulingConflictResult
{
    public string ConflictType { get; init; } = "";
    public string Message { get; init; } = "";
    public int? ConflictingEntityId { get; init; }
    public DateTime? ConflictStart { get; init; }
    public DateTime? ConflictEnd { get; init; }
    public int EmployeeId { get; init; }
    public string? EmployeeName { get; init; }
    public string? Details { get; init; }
}
