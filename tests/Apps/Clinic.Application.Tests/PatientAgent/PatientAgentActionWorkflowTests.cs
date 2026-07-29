using FluentAssertions;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Contracts.PatientAgent;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.PatientAgent;

public sealed class PatientAgentActionWorkflowTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-01T12:00:00Z");
    private static readonly Guid DefaultPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Confirmation_contract_cannot_resend_business_fields()
    {
        typeof(ConfirmPatientAgentActionRequest).GetProperties().Select(property => property.Name)
            .Should().Equal(nameof(ConfirmPatientAgentActionRequest.ActionId));
    }

    [Theory]
    [InlineData("create_appointment", "Appointments.Create")]
    [InlineData("create_prescription", "Prescriptions.Create")]
    [InlineData("add_clinical_event", "ClinicalEvents.Create")]
    public async Task Confirmation_completes_each_supported_action(string actionType, string permission)
    {
        InMemoryActionStore store = new(Action(actionType));
        StubCommitter committer = new(actionType, PatientAgentActionCommitResult.Completed("{\"id\":\"completed\"}"));
        ConfirmPatientAgentActionHandler handler = Handler(store, committer);

        PatientAgentActionResult result = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { permission })), CancellationToken.None);

        result.Outcome.Should().Be(PatientAgentActionOutcome.Succeeded);
        result.Action!.Status.Should().Be(PatientAgentActionStatus.Completed);
        result.Action.CompletionPayload.Should().Be("{\"id\":\"completed\"}");
        committer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Rejection_is_terminal_and_does_not_invoke_committer()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", PatientAgentActionCommitResult.Completed("{}"));
        RejectPatientAgentActionHandler reject = new(store, new FixedTimeProvider(Now));

        PatientAgentActionResult result = await reject.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }), "  not requested  "), CancellationToken.None);
        _ = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        result.Action!.Status.Should().Be(PatientAgentActionStatus.Rejected);
        result.Action.RejectionReason.Should().Be("not requested");
        committer.InvocationCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true, false, PatientAgentActionOutcome.ScopeMismatch)]
    [InlineData(false, true, PatientAgentActionOutcome.ScopeMismatch)]
    [InlineData(false, false, PatientAgentActionOutcome.PermissionDenied)]
    public async Task Confirmation_enforces_patient_practice_and_user_scope(
        bool wrongPatient, bool wrongPractice, PatientAgentActionOutcome expected)
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        PatientAgentContext context = Context(
            patientId: wrongPatient ? Guid.NewGuid() : store.Action.PatientId,
            practiceId: wrongPractice ? "other-practice" : store.Action.PracticeId,
            userId: expected == PatientAgentActionOutcome.PermissionDenied ? "other-user" : store.Action.RequestedByUserId,
            permissions: new HashSet<string> { "Appointments.Create" });

        PatientAgentActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(store.Action.ActionId, context), CancellationToken.None);

        result.Outcome.Should().Be(expected);
        store.Action.Status.Should().Be(PatientAgentActionStatus.Proposed);
    }

    [Fact]
    public async Task Missing_capability_permission_is_denied()
    {
        InMemoryActionStore store = new(Action("create_prescription"));
        PatientAgentActionResult result = await Handler(store, new StubCommitter("create_prescription")).Handle(
            new(store.Action.ActionId, Context()), CancellationToken.None);
        result.Outcome.Should().Be(PatientAgentActionOutcome.PermissionDenied);
    }

    [Fact]
    public async Task Unknown_action_is_not_found()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        PatientAgentActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(Guid.NewGuid(), Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(PatientAgentActionOutcome.NotFound);
    }

    [Fact]
    public async Task Expired_action_is_marked_expired_without_committing()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ExpiresAt = Now });
        StubCommitter committer = new("create_appointment");
        PatientAgentActionResult result = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(PatientAgentActionOutcome.Expired);
        result.Action!.Status.Should().Be(PatientAgentActionStatus.Expired);
        committer.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Repeated_and_concurrent_confirmation_execute_committer_once()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", PatientAgentActionCommitResult.Completed("{}"), block: true);
        ConfirmPatientAgentActionHandler handler = Handler(store, committer);
        ConfirmPatientAgentAction command = new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }));

        Task<PatientAgentActionResult> first = handler.Handle(command, CancellationToken.None);
        await committer.Started.Task;
        PatientAgentActionResult concurrent = await handler.Handle(command, CancellationToken.None);
        committer.Release.SetResult();
        PatientAgentActionResult completed = await first;
        PatientAgentActionResult repeated = await handler.Handle(command, CancellationToken.None);

        concurrent.Outcome.Should().Be(PatientAgentActionOutcome.Conflict);
        completed.Outcome.Should().Be(PatientAgentActionOutcome.Succeeded);
        repeated.Outcome.Should().Be(PatientAgentActionOutcome.Succeeded);
        committer.InvocationCount.Should().Be(1);
    }

    [Theory]
    [InlineData("owning_proposal_not_found", false)]
    [InlineData("stale_owning_proposal", false)]
    [InlineData("temporary_dependency_failure", true)]
    [InlineData("unknown_commit_outcome", false)]
    public async Task Commit_failures_preserve_their_retry_classification(string code, bool retryable)
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", PatientAgentActionCommitResult.Failed(code, "failed", retryable));
        ConfirmPatientAgentActionHandler handler = Handler(store, committer);

        PatientAgentActionResult first = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        PatientAgentActionResult second = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        first.Action!.Failure.Should().BeEquivalentTo(new PatientAgentActionFailure(code, "failed", retryable));
        committer.InvocationCount.Should().Be(retryable ? 2 : 1);
        second.Outcome.Should().Be(retryable ? PatientAgentActionOutcome.Failed : PatientAgentActionOutcome.Conflict);
    }

    private static ConfirmPatientAgentActionHandler Handler(InMemoryActionStore store, params StubCommitter[] committers) =>
        new(store, new PatientAgentActionCommitterRegistry(committers), new FixedTimeProvider(Now));

    private static PatientAgentAction Action(string actionType) => new(
        Guid.NewGuid(), "run-1", "conversation-1", DefaultPatientId, "practice-1", "user-1",
        "Capability", actionType, Guid.NewGuid(), "{}", PatientAgentActionStatus.Proposed,
        Now.AddMinutes(-1), Now.AddMinutes(30));

    private static PatientAgentContext Context(
        Guid? patientId = null,
        string practiceId = "practice-1",
        string userId = "user-1",
        IReadOnlySet<string>? permissions = null) =>
        new(practiceId, patientId ?? DefaultPatientId, userId, permissions ?? new HashSet<string>(),
            "Clinician", "en-US", "UTC", "conversation-1");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubCommitter(
        string actionType,
        PatientAgentActionCommitResult? result = null,
        bool block = false) : IPatientAgentActionCommitter
    {
        public string ActionType => actionType;
        public int InvocationCount { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<PatientAgentActionCommitResult> CommitAsync(
            PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
        {
            InvocationCount++;
            Started.TrySetResult();
            if (block) await Release.Task.WaitAsync(cancellationToken);
            return result ?? PatientAgentActionCommitResult.Completed("{}");
        }
    }

    private sealed class InMemoryActionStore(PatientAgentAction action) : IPatientAgentActionStore
    {
        private readonly object _sync = new();
        public PatientAgentAction Action { get; private set; } = action;

        public Task SaveAsync(PatientAgentAction value, CancellationToken cancellationToken)
        {
            lock (_sync) Action = value;
            return Task.CompletedTask;
        }

        public Task<PatientAgentAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
        {
            lock (_sync) return Task.FromResult<PatientAgentAction?>(Action.ActionId == actionId ? Action : null);
        }

        public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == PatientAgentActionStatus.Proposed ||
                retryFailed && current.Status == PatientAgentActionStatus.Failed && current.Failure?.Retryable == true,
                current => current with { Status = PatientAgentActionStatus.Confirming, Failure = null });

        public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == PatientAgentActionStatus.Proposed,
                current => current with { Status = PatientAgentActionStatus.Rejected, RejectedAt = rejectedAt, RejectionReason = reason });

        public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == PatientAgentActionStatus.Confirming,
                current => current with { Status = PatientAgentActionStatus.Completed, ConfirmedAt = confirmedAt, ConfirmedByUserId = confirmedByUserId, CompletionPayload = completionPayload });

        public Task<bool> FailAsync(Guid actionId, PatientAgentActionFailure failure, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status is PatientAgentActionStatus.Confirming or PatientAgentActionStatus.Proposed,
                current => current with { Status = PatientAgentActionStatus.Failed, Failure = failure });

        public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status is PatientAgentActionStatus.Proposed or PatientAgentActionStatus.Failed,
                current => current with { Status = PatientAgentActionStatus.Expired });

        private Task<bool> Transition(Guid actionId, Func<PatientAgentAction, bool> predicate, Func<PatientAgentAction, PatientAgentAction> update)
        {
            lock (_sync)
            {
                if (Action.ActionId != actionId || !predicate(Action)) return Task.FromResult(false);
                Action = update(Action);
                return Task.FromResult(true);
            }
        }
    }
}
