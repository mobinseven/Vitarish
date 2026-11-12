using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Intelligence.Tests.PreparedActions;

public sealed class PreparedActionLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActionId = Guid.Parse("6f1d1f0e-6a0a-4c53-9a77-0e4b6a1d2c11");

    private readonly FakeTimeProvider _time = new(Now);
    private readonly InMemoryPreparedActionStore _store = new();
    private readonly FakeApp _app = new();
    private readonly PreparedActionLifecycle _lifecycle;

    public PreparedActionLifecycleTests() => _lifecycle = new(_store, _time);

    public static TheoryData<string> MismatchedFields => new("agent", "scope", "kind", "subject");

    private static PreparedAction Action(
        PreparedActionStatus status = PreparedActionStatus.Proposed,
        PreparedActionFailure? failure = null,
        DateTimeOffset? expiresAt = null,
        int policyRevision = 1) => new(
        ActionId,
        "agent-a",
        "scope-1",
        "patient",
        "subject-1",
        "conversation-1",
        "run-1",
        "user-1",
        "capability-1",
        "action-type",
        policyRevision,
        "review-payload",
        "summary",
        "tool-call-1",
        "tool-name",
        status,
        Now.AddMinutes(-5),
        expiresAt ?? Now.AddMinutes(30),
        Failure: failure);

    private static PreparedAction WithMismatch(PreparedAction action, string field) => field switch
    {
        "agent" => action with { AgentKey = "agent-b" },
        "scope" => action with { ScopeId = "scope-2" },
        "kind" => action with { SubjectKind = "visit" },
        "subject" => action with { SubjectId = "subject-2" },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private PreparedAction Stored() => _store.Current(ActionId)!;

    // ---------- 1. Not found ----------

    [Fact]
    public async Task Get_confirm_and_reject_of_a_missing_action_are_not_found()
    {
        (await _lifecycle.GetAsync(ActionId, _app, default)).Should().Be(new PreparedActionResult(PreparedActionOutcome.NotFound));
        (await _lifecycle.ConfirmAsync(ActionId, _app, default)).Should().Be(new PreparedActionResult(PreparedActionOutcome.NotFound));
        (await _lifecycle.RejectAsync(ActionId, _app, null, default)).Should().Be(new PreparedActionResult(PreparedActionOutcome.NotFound));
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 2. Scope mismatch ----------

    [Theory]
    [MemberData(nameof(MismatchedFields))]
    public async Task Get_with_a_different_agent_scope_or_subject_is_scope_mismatch(string field)
    {
        PreparedAction original = WithMismatch(Action(), field);
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.GetAsync(ActionId, _app, default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.ScopeMismatch));
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(MismatchedFields))]
    public async Task Confirm_with_a_different_agent_scope_or_subject_is_scope_mismatch(string field)
    {
        PreparedAction original = WithMismatch(Action(), field);
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.ScopeMismatch));
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(MismatchedFields))]
    public async Task Reject_with_a_different_agent_scope_or_subject_is_scope_mismatch(string field)
    {
        PreparedAction original = WithMismatch(Action(), field);
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, "reason", default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.ScopeMismatch));
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 3. Permission denied ----------

    [Fact]
    public async Task A_different_requesting_user_is_permission_denied_for_get_confirm_and_reject()
    {
        PreparedAction original = Action() with { RequestedByUserId = "user-2" };
        _store.Seed(original);

        (await _lifecycle.GetAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.ConfirmAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.RejectAsync(ActionId, _app, "reason", default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task A_missing_policy_is_permission_denied_for_get_confirm_and_reject()
    {
        PreparedAction original = Action();
        _store.Seed(original);
        _app.Policy = null;

        (await _lifecycle.GetAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.ConfirmAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.RejectAsync(ActionId, _app, "reason", default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task An_unauthorized_caller_is_permission_denied_for_get_confirm_and_reject()
    {
        PreparedAction original = Action();
        _store.Seed(original);
        _app.Authorized = false;

        (await _lifecycle.GetAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.ConfirmAsync(ActionId, _app, default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        (await _lifecycle.RejectAsync(ActionId, _app, "reason", default)).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task Get_returns_the_stored_action_to_its_owner()
    {
        PreparedAction original = Action();
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.GetAsync(ActionId, _app, default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.Succeeded, original));
        Stored().Should().Be(original);
    }

    // ---------- 4. Confirm happy path ----------

    [Fact]
    public async Task Confirm_completes_the_action_with_the_commit_payload()
    {
        _store.Seed(Action());
        _app.Result = PreparedActionCommitResult.Completed("""{"id":1}""");
        _time.SetUtcNow(Now.AddMinutes(2));

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        result.Action.Should().Be(Stored());
        PreparedAction stored = Stored();
        stored.Status.Should().Be(PreparedActionStatus.Completed);
        stored.ConfirmedByUserId.Should().Be("user-1");
        stored.ConfirmedAt.Should().Be(Now.AddMinutes(2));
        stored.CompletionPayload.Should().Be("""{"id":1}""");
        stored.Failure.Should().BeNull();
        _app.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task Confirm_stores_an_empty_object_when_the_commit_has_no_payload()
    {
        _store.Seed(Action());
        _app.Result = new PreparedActionCommitResult(true, null, null);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        Stored().Status.Should().Be(PreparedActionStatus.Completed);
        Stored().CompletionPayload.Should().Be("{}");
        _app.CommitCount.Should().Be(1);
    }

    // ---------- 5. Already completed ----------

    [Fact]
    public async Task Confirm_of_a_completed_action_succeeds_without_committing_again()
    {
        PreparedAction completed = Action(PreparedActionStatus.Completed) with
        {
            ConfirmedAt = Now,
            ConfirmedByUserId = "user-1",
            CompletionPayload = "done"
        };
        _store.Seed(completed);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.Succeeded, completed));
        Stored().Should().Be(completed);
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 6. Conflict ----------

    [Theory]
    [InlineData(PreparedActionStatus.Rejected)]
    [InlineData(PreparedActionStatus.Expired)]
    [InlineData(PreparedActionStatus.Failed)]
    public async Task Confirm_of_a_rejected_expired_or_non_retryable_failed_action_is_a_conflict(PreparedActionStatus status)
    {
        PreparedAction original = Action(status, status == PreparedActionStatus.Failed ? new("code", "message", false) : null);
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.Conflict, original));
        Stored().Should().Be(original);
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 7. Expiry ----------

    [Fact]
    public async Task Confirm_of_an_action_past_its_expiry_is_expired_and_stored_as_expired()
    {
        _store.Seed(Action(expiresAt: Now.AddSeconds(-1)));

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Expired);
        result.Action!.Status.Should().Be(PreparedActionStatus.Expired);
        Stored().Status.Should().Be(PreparedActionStatus.Expired);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task An_action_expiring_exactly_now_counts_as_expired()
    {
        _store.Seed(Action(expiresAt: Now));

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Expired);
        Stored().Status.Should().Be(PreparedActionStatus.Expired);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task An_action_expiring_after_now_can_still_be_confirmed()
    {
        _store.Seed(Action(expiresAt: Now.AddTicks(1)));

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        Stored().Status.Should().Be(PreparedActionStatus.Completed);
        _app.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task Reject_of_an_expired_proposed_action_is_expired_and_stored_as_expired()
    {
        _store.Seed(Action(expiresAt: Now.AddMinutes(-1)));

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, "reason", default);

        result.Outcome.Should().Be(PreparedActionOutcome.Expired);
        result.Action!.Status.Should().Be(PreparedActionStatus.Expired);
        Stored().Status.Should().Be(PreparedActionStatus.Expired);
        Stored().RejectedAt.Should().BeNull();
        Stored().RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task Reject_of_a_proposed_action_expiring_exactly_now_is_expired()
    {
        _store.Seed(Action(expiresAt: Now));

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, null, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Expired);
        Stored().Status.Should().Be(PreparedActionStatus.Expired);
    }

    // ---------- 8. Stale policy ----------

    [Fact]
    public async Task Confirm_with_a_newer_policy_revision_fails_as_stale_and_does_not_commit()
    {
        _store.Seed(Action(policyRevision: 1));
        _app.Policy = new(2, PreparedActionRetryClassification.Retryable);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Failed);
        PreparedAction stored = Stored();
        stored.Status.Should().Be(PreparedActionStatus.Failed);
        stored.Failure.Should().NotBeNull();
        stored.Failure!.Code.Should().Be(PreparedActionFailureCodes.StaleActionPolicy);
        stored.Failure.Retryable.Should().BeFalse();
        result.Action.Should().Be(stored);
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 9. Unsupported ----------

    [Fact]
    public async Task Confirm_of_an_action_type_that_cannot_commit_fails_as_unsupported()
    {
        _store.Seed(Action());
        _app.CanCommitAction = false;

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Failed);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        Stored().Failure!.Code.Should().Be(PreparedActionFailureCodes.UnsupportedAction);
        Stored().Failure!.Retryable.Should().BeFalse();
        _app.CommitCount.Should().Be(0);
    }

    // ---------- 10. Retry classification ----------

    [Theory]
    [InlineData(PreparedActionRetryClassification.Retryable)]
    [InlineData(PreparedActionRetryClassification.Idempotent)]
    public async Task A_retryable_failure_can_be_confirmed_again_when_the_policy_allows_retry(
        PreparedActionRetryClassification classification)
    {
        _store.Seed(Action());
        _app.Policy = new(1, classification);
        _app.Result = PreparedActionCommitResult.Failed("workflow_busy", "Try later.", retryable: true);

        PreparedActionResult first = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        first.Outcome.Should().Be(PreparedActionOutcome.Failed);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        Stored().Failure.Should().Be(new PreparedActionFailure("workflow_busy", "Try later.", true));
        _app.CommitCount.Should().Be(1);

        _app.Result = PreparedActionCommitResult.Completed("ok");
        PreparedActionResult second = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        second.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        Stored().Status.Should().Be(PreparedActionStatus.Completed);
        Stored().CompletionPayload.Should().Be("ok");
        Stored().Failure.Should().BeNull();
        _app.CommitCount.Should().Be(2);
    }

    [Fact]
    public async Task A_never_retry_policy_forces_the_failure_non_retryable()
    {
        _store.Seed(Action());
        _app.Policy = new(1, PreparedActionRetryClassification.NeverRetry);
        _app.Result = PreparedActionCommitResult.Failed("workflow_busy", "Try later.", retryable: true);

        PreparedActionResult first = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        first.Outcome.Should().Be(PreparedActionOutcome.Failed);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        Stored().Failure.Should().Be(new PreparedActionFailure("workflow_busy", "Try later.", false));
        _app.CommitCount.Should().Be(1);

        PreparedActionResult second = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        second.Outcome.Should().Be(PreparedActionOutcome.Conflict);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        _app.CommitCount.Should().Be(1);
    }

    // ---------- 11. Exceptions ----------

    [Fact]
    public async Task A_throwing_committer_fails_with_an_unknown_non_retryable_outcome()
    {
        _store.Seed(Action());
        _app.Throws = new InvalidOperationException("boom");

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Failed);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        Stored().Failure!.Code.Should().Be(PreparedActionFailureCodes.UnknownCommitOutcome);
        Stored().Failure!.Retryable.Should().BeFalse();
        _app.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task A_cancelled_committer_propagates_the_cancellation()
    {
        _store.Seed(Action());
        _app.Throws = new OperationCanceledException();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        Func<Task> act = () => _lifecycle.ConfirmAsync(ActionId, _app, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _app.CommitCount.Should().Be(1);
    }

    // ---------- 12. Failure missing ----------

    [Fact]
    public async Task A_failed_commit_without_a_failure_is_stored_as_commit_failed()
    {
        _store.Seed(Action());
        _app.Result = new PreparedActionCommitResult(false, null, null);

        PreparedActionResult result = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Failed);
        Stored().Status.Should().Be(PreparedActionStatus.Failed);
        Stored().Failure!.Code.Should().Be(PreparedActionFailureCodes.CommitFailed);
        Stored().Failure!.Retryable.Should().BeFalse();
        _app.CommitCount.Should().Be(1);
    }

    // ---------- 13. Concurrency ----------

    [Fact]
    public async Task A_concurrent_confirm_conflicts_while_the_first_commits_and_the_commit_runs_once()
    {
        _store.Seed(Action());
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _app.Block(release.Task);

        Task<PreparedActionResult> first = _lifecycle.ConfirmAsync(ActionId, _app, default);
        await _app.Started.WaitAsync(TimeSpan.FromSeconds(10));
        Stored().Status.Should().Be(PreparedActionStatus.Confirming);

        PreparedActionResult second = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        second.Outcome.Should().Be(PreparedActionOutcome.Conflict);
        second.Action!.Status.Should().Be(PreparedActionStatus.Confirming);

        release.SetResult();
        PreparedActionResult firstResult = await first;

        firstResult.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        Stored().Status.Should().Be(PreparedActionStatus.Completed);

        PreparedActionResult third = await _lifecycle.ConfirmAsync(ActionId, _app, default);

        third.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        _app.CommitCount.Should().Be(1);
    }

    // ---------- 14. Reject ----------

    [Theory]
    [InlineData("  no  ", "no")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task Reject_normalizes_the_reason(string? reason, string? expected)
    {
        _store.Seed(Action());
        _time.SetUtcNow(Now.AddMinutes(3));

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, reason, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        PreparedAction stored = Stored();
        result.Action.Should().Be(stored);
        stored.Status.Should().Be(PreparedActionStatus.Rejected);
        stored.RejectionReason.Should().Be(expected);
        stored.RejectedAt.Should().Be(Now.AddMinutes(3));
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task Reject_with_a_reason_over_the_limit_is_a_validation_failure_and_changes_nothing()
    {
        PreparedAction original = Action();
        _store.Seed(original);

        PreparedActionResult result = await _lifecycle.RejectAsync(
            ActionId, _app, new string('x', PreparedActionLifecycle.MaxRejectionReasonLength + 1), default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.ValidationFailure));
        Stored().Should().Be(original);
    }

    [Fact]
    public async Task Reject_with_a_reason_at_the_limit_succeeds()
    {
        _store.Seed(Action());
        string reason = new('x', PreparedActionLifecycle.MaxRejectionReasonLength);

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, reason, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Succeeded);
        Stored().RejectionReason.Should().Be(reason);
    }

    [Fact]
    public async Task Reject_of_a_completed_action_is_a_conflict_and_changes_nothing()
    {
        PreparedAction completed = Action(PreparedActionStatus.Completed) with { CompletionPayload = "done" };
        _store.Seed(completed);

        PreparedActionResult result = await _lifecycle.RejectAsync(ActionId, _app, "reason", default);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.Conflict, completed));
        Stored().Should().Be(completed);
    }

    // ---------- 15. Prepare ----------

    [Fact]
    public async Task Prepare_saves_a_proposed_action()
    {
        PreparedAction proposed = Action();

        await _lifecycle.PrepareAsync(proposed, default);

        Stored().Should().Be(proposed);
        _store.SaveCount.Should().Be(1);
    }

    [Theory]
    [InlineData(PreparedActionStatus.Confirming)]
    [InlineData(PreparedActionStatus.Completed)]
    [InlineData(PreparedActionStatus.Rejected)]
    [InlineData(PreparedActionStatus.Expired)]
    [InlineData(PreparedActionStatus.Failed)]
    public async Task Prepare_rejects_an_action_that_is_not_proposed_and_saves_nothing(PreparedActionStatus status)
    {
        Func<Task> act = () => _lifecycle.PrepareAsync(Action(status), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _store.SaveCount.Should().Be(0);
        _store.Current(ActionId).Should().BeNull();
    }

    // ---------- Test doubles ----------

    private sealed class InMemoryPreparedActionStore : IPreparedActionStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, StoredPreparedAction> _actions = [];
        private long _counter;

        public int SaveCount { get; private set; }

        public void Seed(PreparedAction action)
        {
            lock (_gate) _actions[action.ActionId] = new(action, NextVersion());
        }

        public PreparedAction? Current(Guid actionId)
        {
            lock (_gate) return _actions.TryGetValue(actionId, out StoredPreparedAction? stored) ? stored.Action : null;
        }

        public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_actions.TryGetValue(action.ActionId, out StoredPreparedAction? existing))
                {
                    if (existing.Action != action) throw new InvalidOperationException("A different action exists under this ID.");
                    return Task.CompletedTask;
                }
                _actions[action.ActionId] = new(action, NextVersion());
                SaveCount++;
            }
            return Task.CompletedTask;
        }

        public Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
        {
            lock (_gate) return Task.FromResult(_actions.GetValueOrDefault(actionId));
        }

        public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_actions.TryGetValue(current.Action.ActionId, out StoredPreparedAction? existing) ||
                    existing.Version != current.Version)
                    return Task.FromResult(false);
                _actions[current.Action.ActionId] = new(next, NextVersion());
                return Task.FromResult(true);
            }
        }

        private string NextVersion() => (++_counter).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class FakeApp : IPreparedActionApp
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _block;
        private int _commitCount;

        public PreparedActionCaller Caller { get; set; } = new("agent-a", "scope-1", "patient", "subject-1", "user-1");

        public PreparedActionPolicy? Policy { get; set; } = new(1, PreparedActionRetryClassification.Retryable);

        public bool Authorized { get; set; } = true;

        public bool CanCommitAction { get; set; } = true;

        public PreparedActionCommitResult Result { get; set; } = PreparedActionCommitResult.Completed("{}");

        public Exception? Throws { get; set; }

        public int CommitCount => Volatile.Read(ref _commitCount);

        public Task Started => _started.Task;

        public void Block(Task until) => _block = until;

        public PreparedActionPolicy? ResolvePolicy(string actionType) => Policy;

        public bool Authorizes(string actionType) => Authorized;

        public bool CanCommit(string actionType) => CanCommitAction;

        public async Task<PreparedActionCommitResult> CommitAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _commitCount);
            _started.TrySetResult();
            if (_block is not null) await _block;
            if (Throws is not null) throw Throws;
            return Result;
        }
    }
}
