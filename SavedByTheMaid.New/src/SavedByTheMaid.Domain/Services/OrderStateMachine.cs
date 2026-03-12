using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Services;

/// <summary>
/// State machine for OrderStatus.
/// Defines the valid transitions between order states.
/// </summary>
public static class OrderStateMachine
{
    /// <summary>
    /// Dictionary of valid transitions for each order state.
    /// Terminal states (Completed, Cancelled, NoShow) have no transitions.
    /// </summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        // PendingReview: New order pending admin review
        [OrderStatus.PendingReview] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        
        // Draft (deprecated): Behaves the same as PendingReview for compatibility
        #pragma warning disable CS0618 // Type or member is obsolete
        [OrderStatus.Draft] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        #pragma warning restore CS0618
        
        // Confirmed: Confirmed order, can start, be cancelled, or marked NoShow
        [OrderStatus.Confirmed] = new[] { OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow },
        
        // InProgress: Service in progress, can be completed or cancelled
        [OrderStatus.InProgress] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
        
        // Terminal states - no allowed transitions
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.NoShow] = Array.Empty<OrderStatus>()
    };

    /// <summary>
    /// States considered final (no further transitions allowed)
    /// </summary>
    public static readonly OrderStatus[] FinalStates = new[]
    {
        OrderStatus.Completed,
        OrderStatus.Cancelled,
        OrderStatus.NoShow
    };

    /// <summary>
    /// States considered active (order in progress)
    /// </summary>
    public static readonly OrderStatus[] ActiveStates = new[]
    {
        OrderStatus.PendingReview,
        OrderStatus.Confirmed,
        OrderStatus.InProgress
    };

    /// <summary>
    /// Checks whether a state transition is valid.
    /// </summary>
    /// <param name="from">Current order state</param>
    /// <param name="to">Proposed target state</param>
    /// <returns>True if the transition is valid, false otherwise</returns>
    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }

    /// <summary>
    /// Gets all states that can be transitioned to from the current state.
    /// </summary>
    /// <param name="current">Current order state</param>
    /// <returns>Array of allowed states, or empty if it is a terminal state</returns>
    public static OrderStatus[] GetAllowedTransitions(OrderStatus current)
    {
        return ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : Array.Empty<OrderStatus>();
    }

    /// <summary>
    /// Checks whether a state is terminal (no further transitions allowed).
    /// </summary>
    /// <param name="status">State to check</param>
    /// <returns>True if it is a terminal state</returns>
    public static bool IsFinalState(OrderStatus status)
    {
        return FinalStates.Contains(status);
    }

    /// <summary>
    /// Checks whether a state is active (order in progress, not terminal).
    /// </summary>
    /// <param name="status">State to check</param>
    /// <returns>True if it is an active state</returns>
    public static bool IsActiveState(OrderStatus status)
    {
        return ActiveStates.Contains(status);
    }

    /// <summary>
    /// Validates a transition and throws an exception if it is not valid.
    /// </summary>
    /// <param name="from">Current state</param>
    /// <param name="to">Target state</param>
    /// <exception cref="InvalidOperationException">If the transition is not valid</exception>
    public static void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid state transition: {from} -> {to}. " +
                $"Allowed transitions from {from}: [{string.Join(", ", GetAllowedTransitions(from))}]");
        }
    }
}

/// <summary>
/// State machine for MeetStatus.
/// Defines the valid transitions between service appointment states.
/// </summary>
public static class MeetStateMachine
{
    /// <summary>
    /// Dictionary of valid transitions for each appointment state.
    /// </summary>
    private static readonly Dictionary<MeetStatus, MeetStatus[]> ValidTransitions = new()
    {
        // Scheduled: Scheduled appointment, can be assigned, cancelled, or rescheduled
        [MeetStatus.Scheduled] = new[] { MeetStatus.Assigned, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        
        // Assigned: Employee assigned, can start traveling, be cancelled, or rescheduled
        [MeetStatus.Assigned] = new[] { MeetStatus.OnTheWay, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        
        // OnTheWay: Employee on the way, can start service or be cancelled
        [MeetStatus.OnTheWay] = new[] { MeetStatus.InProgress, MeetStatus.Cancelled, MeetStatus.NoShow },
        
        // InProgress: Service in progress, can be completed or cancelled
        [MeetStatus.InProgress] = new[] { MeetStatus.Completed, MeetStatus.Cancelled },
        
        // Rescheduled: Rescheduled, returns to Scheduled state when a new date is confirmed
        [MeetStatus.Rescheduled] = new[] { MeetStatus.Scheduled, MeetStatus.Assigned, MeetStatus.Cancelled },
        
        // Terminal states
        [MeetStatus.Completed] = Array.Empty<MeetStatus>(),
        [MeetStatus.Cancelled] = Array.Empty<MeetStatus>(),
        [MeetStatus.NoShow] = Array.Empty<MeetStatus>()
    };

    /// <summary>
    /// States considered final
    /// </summary>
    public static readonly MeetStatus[] FinalStates = new[]
    {
        MeetStatus.Completed,
        MeetStatus.Cancelled,
        MeetStatus.NoShow
    };

    /// <summary>
    /// States considered active
    /// </summary>
    public static readonly MeetStatus[] ActiveStates = new[]
    {
        MeetStatus.Scheduled,
        MeetStatus.Assigned,
        MeetStatus.OnTheWay,
        MeetStatus.InProgress,
        MeetStatus.Rescheduled
    };

    /// <summary>
    /// Checks whether a state transition is valid.
    /// </summary>
    public static bool CanTransition(MeetStatus from, MeetStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }

    /// <summary>
    /// Gets all states that can be transitioned to from the current state.
    /// </summary>
    public static MeetStatus[] GetAllowedTransitions(MeetStatus current)
    {
        return ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : Array.Empty<MeetStatus>();
    }

    /// <summary>
    /// Checks whether a state is terminal.
    /// </summary>
    public static bool IsFinalState(MeetStatus status)
    {
        return FinalStates.Contains(status);
    }

    /// <summary>
    /// Checks whether a state is active.
    /// </summary>
    public static bool IsActiveState(MeetStatus status)
    {
        return ActiveStates.Contains(status);
    }

    /// <summary>
    /// Validates a transition and throws an exception if it is not valid.
    /// </summary>
    public static void ValidateTransition(MeetStatus from, MeetStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid state transition: {from} -> {to}. " +
                $"Allowed transitions from {from}: [{string.Join(", ", GetAllowedTransitions(from))}]");
        }
    }
}
