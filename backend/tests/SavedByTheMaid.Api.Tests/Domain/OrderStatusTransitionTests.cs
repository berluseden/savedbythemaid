using FluentAssertions;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Tests.Domain;

/// <summary>
/// Tests for the order and meeting status state machine transitions.
/// These transitions are defined in AdminOrdersController and enforced at the API level.
/// This test suite validates them in isolation to ensure correctness.
/// </summary>
public class OrderStatusTransitionTests
{
    /// <summary>
    /// Valid OrderStatus transitions as defined in AdminOrdersController.
    /// This replicates the transition map to test it independently.
    /// </summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidOrderTransitions = new()
    {
        [OrderStatus.PendingReview] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        #pragma warning disable CS0618
        [OrderStatus.Draft] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        #pragma warning restore CS0618
        [OrderStatus.Confirmed] = new[] { OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow },
        [OrderStatus.InProgress] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.NoShow] = Array.Empty<OrderStatus>()
    };

    private static bool IsValidTransition(OrderStatus from, OrderStatus to)
    {
        return ValidOrderTransitions.ContainsKey(from) &&
               ValidOrderTransitions[from].Contains(to);
    }

    #region Valid OrderStatus Transitions

    [Fact]
    public void OrderStatus_PendingReview_To_Confirmed_IsValid()
    {
        IsValidTransition(OrderStatus.PendingReview, OrderStatus.Confirmed)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_PendingReview_To_Cancelled_IsValid()
    {
        IsValidTransition(OrderStatus.PendingReview, OrderStatus.Cancelled)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_Confirmed_To_InProgress_IsValid()
    {
        IsValidTransition(OrderStatus.Confirmed, OrderStatus.InProgress)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_Confirmed_To_Cancelled_IsValid()
    {
        IsValidTransition(OrderStatus.Confirmed, OrderStatus.Cancelled)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_Confirmed_To_NoShow_IsValid()
    {
        IsValidTransition(OrderStatus.Confirmed, OrderStatus.NoShow)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_InProgress_To_Completed_IsValid()
    {
        IsValidTransition(OrderStatus.InProgress, OrderStatus.Completed)
            .Should().BeTrue();
    }

    [Fact]
    public void OrderStatus_InProgress_To_Cancelled_IsValid()
    {
        IsValidTransition(OrderStatus.InProgress, OrderStatus.Cancelled)
            .Should().BeTrue();
    }

    #endregion

    #region Invalid OrderStatus Transitions

    [Fact]
    public void OrderStatus_PendingReview_To_InProgress_IsInvalid()
    {
        IsValidTransition(OrderStatus.PendingReview, OrderStatus.InProgress)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_PendingReview_To_Completed_IsInvalid()
    {
        IsValidTransition(OrderStatus.PendingReview, OrderStatus.Completed)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_PendingReview_To_NoShow_IsInvalid()
    {
        IsValidTransition(OrderStatus.PendingReview, OrderStatus.NoShow)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_Confirmed_To_Completed_IsInvalid()
    {
        // Cannot skip InProgress
        IsValidTransition(OrderStatus.Confirmed, OrderStatus.Completed)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_Confirmed_To_PendingReview_IsInvalid()
    {
        // Cannot go backwards
        IsValidTransition(OrderStatus.Confirmed, OrderStatus.PendingReview)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_InProgress_To_Confirmed_IsInvalid()
    {
        // Cannot go backwards
        IsValidTransition(OrderStatus.InProgress, OrderStatus.Confirmed)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_InProgress_To_NoShow_IsInvalid()
    {
        IsValidTransition(OrderStatus.InProgress, OrderStatus.NoShow)
            .Should().BeFalse();
    }

    [Fact]
    public void OrderStatus_Completed_IsTerminal_NoTransitionsAllowed()
    {
        // Completed is a terminal state
        var allStatuses = Enum.GetValues<OrderStatus>();
        foreach (var targetStatus in allStatuses)
        {
            IsValidTransition(OrderStatus.Completed, targetStatus)
                .Should().BeFalse($"Completed -> {targetStatus} should not be valid");
        }
    }

    [Fact]
    public void OrderStatus_Cancelled_IsTerminal_NoTransitionsAllowed()
    {
        // Cancelled is a terminal state
        var allStatuses = Enum.GetValues<OrderStatus>();
        foreach (var targetStatus in allStatuses)
        {
            IsValidTransition(OrderStatus.Cancelled, targetStatus)
                .Should().BeFalse($"Cancelled -> {targetStatus} should not be valid");
        }
    }

    [Fact]
    public void OrderStatus_NoShow_IsTerminal_NoTransitionsAllowed()
    {
        // NoShow is a terminal state
        var allStatuses = Enum.GetValues<OrderStatus>();
        foreach (var targetStatus in allStatuses)
        {
            IsValidTransition(OrderStatus.NoShow, targetStatus)
                .Should().BeFalse($"NoShow -> {targetStatus} should not be valid");
        }
    }

    #endregion

    #region OrderStatus Transition Coverage

    [Theory]
    [InlineData(OrderStatus.PendingReview, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.PendingReview, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.PendingReview, OrderStatus.InProgress, false)]
    [InlineData(OrderStatus.PendingReview, OrderStatus.Completed, false)]
    [InlineData(OrderStatus.PendingReview, OrderStatus.NoShow, false)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.InProgress, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.NoShow, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed, false)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.PendingReview, false)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.InProgress, OrderStatus.NoShow, false)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Completed, OrderStatus.InProgress, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.PendingReview, false)]
    [InlineData(OrderStatus.NoShow, OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.NoShow, OrderStatus.Cancelled, false)]
    public void OrderStatus_TransitionMatrix(OrderStatus from, OrderStatus to, bool expectedValid)
    {
        IsValidTransition(from, to).Should().Be(expectedValid,
            $"transition from {from} to {to} should be {(expectedValid ? "valid" : "invalid")}");
    }

    #endregion

    #region All States Have Transition Rules

    [Fact]
    public void OrderStatus_AllStatesHaveTransitionRules()
    {
        var allStatuses = Enum.GetValues<OrderStatus>();
        foreach (var status in allStatuses)
        {
            ValidOrderTransitions.Should().ContainKey(status,
                $"status {status} should have a transition rule defined");
        }
    }

    [Fact]
    public void OrderStatus_SelfTransitions_AreNotAllowed()
    {
        // No status should be able to transition to itself
        var allStatuses = Enum.GetValues<OrderStatus>();
        foreach (var status in allStatuses)
        {
            IsValidTransition(status, status)
                .Should().BeFalse($"self-transition {status} -> {status} should not be valid");
        }
    }

    #endregion

    #region MeetStatus Transitions

    /// <summary>
    /// Valid MeetStatus transitions. These are less strictly enforced in the controller
    /// (UpdateMeetingStatus doesn't validate transitions), but we document the expected flow.
    /// </summary>
    private static readonly Dictionary<MeetStatus, MeetStatus[]> ExpectedMeetTransitions = new()
    {
        [MeetStatus.Scheduled] = new[] { MeetStatus.Assigned, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        [MeetStatus.Assigned] = new[] { MeetStatus.OnTheWay, MeetStatus.InProgress, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        [MeetStatus.OnTheWay] = new[] { MeetStatus.InProgress, MeetStatus.Cancelled },
        [MeetStatus.InProgress] = new[] { MeetStatus.Completed, MeetStatus.Cancelled },
        [MeetStatus.Completed] = Array.Empty<MeetStatus>(),
        [MeetStatus.Cancelled] = Array.Empty<MeetStatus>(),
        [MeetStatus.Rescheduled] = new[] { MeetStatus.Scheduled, MeetStatus.Assigned, MeetStatus.Cancelled },
        [MeetStatus.NoShow] = Array.Empty<MeetStatus>()
    };

    private static bool IsValidMeetTransition(MeetStatus from, MeetStatus to)
    {
        return ExpectedMeetTransitions.ContainsKey(from) &&
               ExpectedMeetTransitions[from].Contains(to);
    }

    [Theory]
    [InlineData(MeetStatus.Scheduled, MeetStatus.Assigned, true)]
    [InlineData(MeetStatus.Scheduled, MeetStatus.Cancelled, true)]
    [InlineData(MeetStatus.Scheduled, MeetStatus.Rescheduled, true)]
    [InlineData(MeetStatus.Scheduled, MeetStatus.Completed, false)]
    [InlineData(MeetStatus.Assigned, MeetStatus.OnTheWay, true)]
    [InlineData(MeetStatus.Assigned, MeetStatus.InProgress, true)]
    [InlineData(MeetStatus.Assigned, MeetStatus.Cancelled, true)]
    [InlineData(MeetStatus.Assigned, MeetStatus.Rescheduled, true)]
    [InlineData(MeetStatus.OnTheWay, MeetStatus.InProgress, true)]
    [InlineData(MeetStatus.OnTheWay, MeetStatus.Cancelled, true)]
    [InlineData(MeetStatus.OnTheWay, MeetStatus.Completed, false)]
    [InlineData(MeetStatus.InProgress, MeetStatus.Completed, true)]
    [InlineData(MeetStatus.InProgress, MeetStatus.Cancelled, true)]
    [InlineData(MeetStatus.InProgress, MeetStatus.Scheduled, false)]
    [InlineData(MeetStatus.Completed, MeetStatus.Cancelled, false)]
    [InlineData(MeetStatus.Completed, MeetStatus.Scheduled, false)]
    [InlineData(MeetStatus.Cancelled, MeetStatus.Scheduled, false)]
    [InlineData(MeetStatus.Cancelled, MeetStatus.Completed, false)]
    [InlineData(MeetStatus.NoShow, MeetStatus.Scheduled, false)]
    [InlineData(MeetStatus.NoShow, MeetStatus.Completed, false)]
    [InlineData(MeetStatus.Rescheduled, MeetStatus.Scheduled, true)]
    [InlineData(MeetStatus.Rescheduled, MeetStatus.Assigned, true)]
    [InlineData(MeetStatus.Rescheduled, MeetStatus.Cancelled, true)]
    public void MeetStatus_TransitionMatrix(MeetStatus from, MeetStatus to, bool expectedValid)
    {
        IsValidMeetTransition(from, to).Should().Be(expectedValid,
            $"meet transition from {from} to {to} should be {(expectedValid ? "valid" : "invalid")}");
    }

    [Fact]
    public void MeetStatus_TerminalStates_HaveNoOutgoingTransitions()
    {
        var terminalStates = new[] { MeetStatus.Completed, MeetStatus.Cancelled, MeetStatus.NoShow };
        var allStatuses = Enum.GetValues<MeetStatus>();

        foreach (var terminal in terminalStates)
        {
            foreach (var target in allStatuses)
            {
                IsValidMeetTransition(terminal, target)
                    .Should().BeFalse($"{terminal} is terminal and should not transition to {target}");
            }
        }
    }

    [Fact]
    public void MeetStatus_AllStatesHaveTransitionRules()
    {
        var allStatuses = Enum.GetValues<MeetStatus>();
        foreach (var status in allStatuses)
        {
            ExpectedMeetTransitions.Should().ContainKey(status,
                $"meet status {status} should have a transition rule defined");
        }
    }

    #endregion
}
