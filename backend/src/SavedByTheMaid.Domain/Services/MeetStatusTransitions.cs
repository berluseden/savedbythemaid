using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Domain.Errors;

namespace SavedByTheMaid.Domain.Services;

/// <summary>
/// Defines valid meeting (appointment) status transitions using the Result pattern.
/// Extracted from the MeetStateMachine and AdminOrdersController logic.
/// </summary>
public static class MeetStatusTransitions
{
    private static readonly Dictionary<MeetStatus, MeetStatus[]> ValidTransitions = new()
    {
        [MeetStatus.Scheduled] = [MeetStatus.Assigned, MeetStatus.Cancelled, MeetStatus.Rescheduled],
        [MeetStatus.Assigned] = [MeetStatus.OnTheWay, MeetStatus.Cancelled, MeetStatus.Rescheduled],
        [MeetStatus.OnTheWay] = [MeetStatus.InProgress, MeetStatus.Cancelled, MeetStatus.NoShow],
        [MeetStatus.InProgress] = [MeetStatus.Completed, MeetStatus.Cancelled],
        [MeetStatus.Rescheduled] = [MeetStatus.Scheduled, MeetStatus.Assigned, MeetStatus.Cancelled],
        [MeetStatus.Completed] = [],
        [MeetStatus.Cancelled] = [],
        [MeetStatus.NoShow] = []
    };

    /// <summary>
    /// Terminal states from which no further transitions are allowed.
    /// </summary>
    public static readonly MeetStatus[] FinalStates =
    [
        MeetStatus.Completed,
        MeetStatus.Cancelled,
        MeetStatus.NoShow
    ];

    /// <summary>
    /// Active states indicating the meeting is still in progress.
    /// </summary>
    public static readonly MeetStatus[] ActiveStates =
    [
        MeetStatus.Scheduled,
        MeetStatus.Assigned,
        MeetStatus.OnTheWay,
        MeetStatus.InProgress,
        MeetStatus.Rescheduled
    ];

    /// <summary>
    /// Checks whether a transition from one status to another is valid.
    /// </summary>
    public static bool IsValid(MeetStatus from, MeetStatus to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>
    /// Validates a transition and returns a Result indicating success or failure.
    /// </summary>
    public static Result Validate(MeetStatus from, MeetStatus to)
        => IsValid(from, to)
            ? Result.Success()
            : Result.Failure(new Error(
                "Meeting.InvalidStatusTransition",
                $"Invalid meeting status transition from {from} to {to}."));

    /// <summary>
    /// Gets all statuses that can be transitioned to from the given status.
    /// </summary>
    public static MeetStatus[] GetAllowedTransitions(MeetStatus current)
        => ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : [];

    /// <summary>
    /// Returns true if the status is a terminal (final) state.
    /// </summary>
    public static bool IsFinalState(MeetStatus status)
        => FinalStates.Contains(status);

    /// <summary>
    /// Returns true if the status is an active (non-terminal) state.
    /// </summary>
    public static bool IsActiveState(MeetStatus status)
        => ActiveStates.Contains(status);
}
