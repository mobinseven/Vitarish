using FluentAssertions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Intelligence.Tests.PreparedActions;

public sealed class PreparedActionTransitionTests
{
    private static readonly DateTimeOffset PreparedAt = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = PreparedAt.AddMinutes(30);
    private static readonly DateTimeOffset TransitionAt = PreparedAt.AddMinutes(5);

    public static TheoryData<PreparedActionStatus> AllStatuses => new(Enum.GetValues<PreparedActionStatus>());

    private static PreparedAction ActionIn(PreparedActionStatus status, PreparedActionFailure? failure = null) => new(
        Guid.Parse("6f1d1f0e-6a0a-4c53-9a77-0e4b6a1d2c11"),
        "agent-key",
        "scope-1",
        "subject-kind",
        "subject-1",
        "conversation-1",
        "run-1",
        "user-1",
        "capability-1",
        "action-type",
        7,
        "review-payload",
        "summary",
        "tool-call-1",
        "tool-name",
        status,
        PreparedAt,
        ExpiresAt,
        Failure: failure);

    private static PreparedAction FailedAction(bool retryable) =>
        ActionIn(PreparedActionStatus.Failed, new("failure-code", "failure-message", retryable));

    private static PreparedAction ActionWithRetryableFailureIfFailed(PreparedActionStatus status) =>
        status == PreparedActionStatus.Failed ? FailedAction(retryable: true) : ActionIn(status);

    private static void ProposalShouldBeUnchanged(PreparedAction next, PreparedAction original) =>
        next.Should().BeEquivalentTo(original, options => options
            .Excluding(action => action.Status)
            .Excluding(action => action.ConfirmedAt)
            .Excluding(action => action.ConfirmedByUserId)
            .Excluding(action => action.RejectedAt)
            .Excluding(action => action.RejectionReason)
            .Excluding(action => action.Failure)
            .Excluding(action => action.CompletionPayload));

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void BeginConfirmation_without_retry_is_allowed_only_from_proposed(PreparedActionStatus status)
    {
        PreparedAction action = ActionWithRetryableFailureIfFailed(status);

        PreparedAction? next = action.BeginConfirmation(retryFailed: false);

        if (status == PreparedActionStatus.Proposed)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Confirming);
            next.Failure.Should().BeNull();
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void BeginConfirmation_with_retry_is_allowed_from_proposed_and_retryable_failed(PreparedActionStatus status)
    {
        PreparedAction action = ActionWithRetryableFailureIfFailed(status);

        PreparedAction? next = action.BeginConfirmation(retryFailed: true);

        if (status is PreparedActionStatus.Proposed or PreparedActionStatus.Failed)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Confirming);
            next.Failure.Should().BeNull();
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BeginConfirmation_from_non_retryable_failed_is_refused(bool retryFailed)
    {
        FailedAction(retryable: false).BeginConfirmation(retryFailed).Should().BeNull();
    }

    [Fact]
    public void BeginConfirmation_from_failed_without_failure_detail_is_refused()
    {
        ActionIn(PreparedActionStatus.Failed).BeginConfirmation(retryFailed: true).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void Complete_is_allowed_only_from_confirming(PreparedActionStatus status)
    {
        PreparedAction action = ActionIn(status);

        PreparedAction? next = action.Complete(TransitionAt, "confirmer-1", "completion-payload");

        if (status == PreparedActionStatus.Confirming)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Completed);
            next.ConfirmedAt.Should().Be(TransitionAt);
            next.ConfirmedByUserId.Should().Be("confirmer-1");
            next.CompletionPayload.Should().Be("completion-payload");
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void Fail_is_allowed_only_from_proposed_and_confirming(PreparedActionStatus status)
    {
        PreparedAction action = ActionIn(status);
        PreparedActionFailure failure = new("failure-code", "failure-message", true);

        PreparedAction? next = action.Fail(failure);

        if (status is PreparedActionStatus.Proposed or PreparedActionStatus.Confirming)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Failed);
            next.Failure.Should().Be(failure);
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void Reject_is_allowed_only_from_proposed(PreparedActionStatus status)
    {
        PreparedAction action = ActionIn(status);

        PreparedAction? next = action.Reject(TransitionAt, "rejection-reason");

        if (status == PreparedActionStatus.Proposed)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Rejected);
            next.RejectedAt.Should().Be(TransitionAt);
            next.RejectionReason.Should().Be("rejection-reason");
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Fact]
    public void Reject_without_a_reason_keeps_the_reason_null()
    {
        PreparedAction? next = ActionIn(PreparedActionStatus.Proposed).Reject(TransitionAt, null);

        next.Should().NotBeNull();
        next!.RejectionReason.Should().BeNull();
        next.RejectedAt.Should().Be(TransitionAt);
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void Expire_is_allowed_only_from_proposed_and_failed(PreparedActionStatus status)
    {
        PreparedAction action = ActionIn(status);

        PreparedAction? next = action.Expire();

        if (status is PreparedActionStatus.Proposed or PreparedActionStatus.Failed)
        {
            next.Should().NotBeNull();
            next!.Status.Should().Be(PreparedActionStatus.Expired);
            ProposalShouldBeUnchanged(next, action);
        }
        else next.Should().BeNull();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsExpiredAt_is_true_from_the_expiry_instant_onward(int secondsFromExpiry, bool expected)
    {
        ActionIn(PreparedActionStatus.Proposed).IsExpiredAt(ExpiresAt.AddSeconds(secondsFromExpiry)).Should().Be(expected);
    }
}
