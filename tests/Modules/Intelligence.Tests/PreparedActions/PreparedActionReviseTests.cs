using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Intelligence.Tests.PreparedActions;

public sealed class PreparedActionReviseTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActionId = Guid.Parse("6f1d1f0e-6a0a-4c53-9a77-0e4b6a1d2c11");
    private static readonly Guid ReplacementId = Guid.Parse("7a2e2a1f-7b1b-4d64-8b88-1f5c7b2e3d22");

    private readonly FakeTimeProvider _time = new(Now);
    private readonly InMemoryPreparedActionStore _store = new();
    private readonly FakeApp _app = new();
    private readonly PreparedActionLifecycle _lifecycle;
    private int _prepareCalls;

    public PreparedActionReviseTests() => _lifecycle = new(_store, _time);

    public static TheoryData<string> MismatchedFields => new("agent", "scope", "kind", "subject");

    public static TheoryData<string> InvalidReplacements => new(
        "status", "sameId", "agentKey", "scope", "subjectKind", "subjectId", "requestedBy", "run", "conversation", "capability", "actionType");

    private static PreparedAction Action(PreparedActionStatus status = PreparedActionStatus.Proposed, DateTimeOffset? expiresAt = null) => new(
        ActionId, "agent-a", "scope-1", "patient", "subject-1", "conversation-1", "run-1", "user-1", "capability-1", "action-type",
        1, "review-payload", "summary", "tool-call-1", "tool-name", status, Now.AddMinutes(-5), expiresAt ?? Now.AddMinutes(30));

    private static PreparedAction Replacement() => Action() with { ActionId = ReplacementId, ReviewPayload = "edited-payload" };

    private static PreparedAction WithMismatch(PreparedAction action, string field) => field switch
    {
        "agent" => action with { AgentKey = "agent-b" },
        "scope" => action with { ScopeId = "scope-2" },
        "kind" => action with { SubjectKind = "visit" },
        "subject" => action with { SubjectId = "subject-2" },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private static PreparedAction Invalidate(PreparedAction replacement, string field) => field switch
    {
        "status" => replacement with { Status = PreparedActionStatus.Rejected },
        "sameId" => replacement with { ActionId = ActionId },
        "agentKey" => replacement with { AgentKey = "agent-b" },
        "scope" => replacement with { ScopeId = "scope-2" },
        "subjectKind" => replacement with { SubjectKind = "visit" },
        "subjectId" => replacement with { SubjectId = "subject-2" },
        "requestedBy" => replacement with { RequestedByUserId = "user-2" },
        "run" => replacement with { AgentRunId = "run-2" },
        "conversation" => replacement with { ConversationId = "conversation-2" },
        "capability" => replacement with { Capability = "capability-2" },
        "actionType" => replacement with { ActionType = "other-type" },
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private Task<PreparedAction?> Prepare(PreparedAction? replacement)
    {
        _prepareCalls++;
        return Task.FromResult(replacement);
    }

    private Task<PreparedActionResult> Revise(PreparedAction? replacement) =>
        _lifecycle.ReviseAsync(ActionId, _app, (_, _) => Prepare(replacement), default);

    private PreparedAction Stored() => _store.Current(ActionId)!;

    [Fact]
    public async Task Revising_a_missing_action_is_not_found()
    {
        (await Revise(Replacement())).Should().Be(new PreparedActionResult(PreparedActionOutcome.NotFound));
        _prepareCalls.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(MismatchedFields))]
    public async Task Revising_with_a_different_agent_scope_or_subject_is_scope_mismatch(string field)
    {
        PreparedAction original = WithMismatch(Action(), field);
        _store.Seed(original);

        (await Revise(Replacement())).Should().Be(new PreparedActionResult(PreparedActionOutcome.ScopeMismatch));

        Stored().Should().Be(original);
        _prepareCalls.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Fact]
    public async Task Revising_as_a_different_requesting_user_is_permission_denied()
    {
        PreparedAction original = Action() with { RequestedByUserId = "user-2" };
        _store.Seed(original);

        (await Revise(Replacement())).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);

        Stored().Should().Be(original);
        _prepareCalls.Should().Be(0);
    }

    [Fact]
    public async Task Revising_without_a_policy_or_authorization_is_permission_denied()
    {
        PreparedAction original = Action();
        _store.Seed(original);

        _app.Policy = null;
        (await Revise(Replacement())).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);
        _app.Policy = new(1, PreparedActionRetryClassification.Retryable);
        _app.Authorized = false;
        (await Revise(Replacement())).Outcome.Should().Be(PreparedActionOutcome.PermissionDenied);

        Stored().Should().Be(original);
        _prepareCalls.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Fact]
    public async Task Revising_an_expired_action_is_expired_and_stored_as_expired()
    {
        _store.Seed(Action(expiresAt: Now.AddSeconds(-1)));

        PreparedActionResult result = await Revise(Replacement());

        result.Outcome.Should().Be(PreparedActionOutcome.Expired);
        result.Action!.Status.Should().Be(PreparedActionStatus.Expired);
        Stored().Status.Should().Be(PreparedActionStatus.Expired);
        _prepareCalls.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Theory]
    [InlineData(PreparedActionStatus.Confirming)]
    [InlineData(PreparedActionStatus.Completed)]
    [InlineData(PreparedActionStatus.Rejected)]
    [InlineData(PreparedActionStatus.Expired)]
    [InlineData(PreparedActionStatus.Failed)]
    public async Task Revising_a_non_proposed_action_is_a_conflict(PreparedActionStatus status)
    {
        PreparedAction original = Action(status);
        _store.Seed(original);

        (await Revise(Replacement())).Should().Be(new PreparedActionResult(PreparedActionOutcome.Conflict, original));

        Stored().Should().Be(original);
        _prepareCalls.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Fact]
    public async Task A_replacement_that_cannot_be_prepared_is_a_validation_failure_and_changes_nothing()
    {
        PreparedAction original = Action();
        _store.Seed(original);

        PreparedActionResult result = await Revise(null);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.ValidationFailure, original));
        Stored().Should().Be(original);
        Stored().Status.Should().Be(PreparedActionStatus.Proposed);
        _store.SaveCount.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(InvalidReplacements))]
    public async Task A_replacement_that_differs_from_the_original_identity_throws_and_changes_nothing(string field)
    {
        PreparedAction original = Action();
        _store.Seed(original);

        Func<Task> act = () => Revise(Invalidate(Replacement(), field));

        await act.Should().ThrowAsync<InvalidOperationException>();
        Stored().Should().Be(original);
        _store.SaveCount.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

    [Fact]
    public async Task A_valid_revision_rejects_the_original_as_revised_and_saves_the_replacement_as_proposed()
    {
        _store.Seed(Action());
        PreparedAction replacement = Replacement();
        _time.SetUtcNow(Now.AddMinutes(2));

        PreparedActionResult result = await Revise(replacement);

        result.Should().Be(new PreparedActionResult(PreparedActionOutcome.Succeeded, replacement));
        PreparedAction original = Stored();
        original.Status.Should().Be(PreparedActionStatus.Rejected);
        original.RejectionReason.Should().Be("revised");
        original.RejectionReason.Should().Be(PreparedActionRejectionReasons.Revised);
        original.RejectedAt.Should().Be(Now.AddMinutes(2));
        _store.Current(ReplacementId).Should().Be(replacement);
        _store.Current(ReplacementId)!.Status.Should().Be(PreparedActionStatus.Proposed);
        _app.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task The_prepare_delegate_receives_the_stored_original()
    {
        PreparedAction original = Action();
        _store.Seed(original);
        PreparedAction? seen = null;

        await _lifecycle.ReviseAsync(ActionId, _app, (current, _) =>
        {
            seen = current;
            return Task.FromResult<PreparedAction?>(Replacement());
        }, default);

        seen.Should().Be(original);
    }

    [Fact]
    public async Task An_original_changed_while_the_replacement_was_prepared_is_a_conflict_and_the_replacement_is_not_saved()
    {
        _store.Seed(Action());

        PreparedActionResult result = await _lifecycle.ReviseAsync(ActionId, _app, async (_, ct) =>
        {
            (await _lifecycle.RejectAsync(ActionId, _app, "someone else", ct)).Outcome.Should().Be(PreparedActionOutcome.Succeeded);
            return Replacement();
        }, default);

        result.Outcome.Should().Be(PreparedActionOutcome.Conflict);
        result.Action!.Status.Should().Be(PreparedActionStatus.Rejected);
        Stored().RejectionReason.Should().Be("someone else");
        _store.SaveCount.Should().Be(0);
        _store.Current(ReplacementId).Should().BeNull();
    }

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
        public PreparedActionCaller Caller { get; set; } = new("agent-a", "scope-1", "patient", "subject-1", "user-1");

        public PreparedActionPolicy? Policy { get; set; } = new(1, PreparedActionRetryClassification.Retryable);

        public bool Authorized { get; set; } = true;

        public int CommitCount { get; private set; }

        public PreparedActionPolicy? ResolvePolicy(string actionType) => Policy;

        public bool Authorizes(string actionType) => Authorized;

        public bool CanCommit(string actionType) => true;

        public Task<PreparedActionCommitResult> CommitAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.FromResult(PreparedActionCommitResult.Completed("{}"));
        }
    }
}
