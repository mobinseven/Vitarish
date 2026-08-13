using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Intelligence.Application.Agents;
using Xunit;
using ClinicWorkspaceActionStatus = Vitarish.Clinic.Application.ClinicWorkspace.ClinicWorkspaceActionStatus;

namespace Vitarish.Clinic.Application.Tests.ClinicWorkspace;

public sealed class ClinicWorkspaceActionWorkflowTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-01T12:00:00Z");
    private static readonly Guid DefaultPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Confirmation_contract_cannot_resend_business_fields()
    {
        typeof(ConfirmClinicAssistantActionRequest).GetProperties().Select(property => property.Name)
            .Should().Equal(nameof(ConfirmClinicAssistantActionRequest.ActionId));
    }

    [Theory]
    [InlineData("create_appointment", "Appointments.Create")]
    [InlineData("create_prescription", "Prescriptions.Create")]
    [InlineData("add_clinical_event", "ClinicalEvents.Create")]
    public async Task Confirmation_completes_each_supported_action(string actionType, string permission)
    {
        InMemoryActionStore store = new(Action(actionType));
        StubCommitter committer = new(actionType, ClinicWorkspaceActionCommitResult.Completed("{\"id\":\"completed\"}"));
        ConfirmClinicWorkspaceActionHandler handler = Handler(store, committer);

        ClinicWorkspaceActionResult result = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { permission })), CancellationToken.None);

        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Succeeded);
        result.Action!.Status.Should().Be(ClinicWorkspaceActionStatus.Completed);
        result.Action.CompletionPayload.Should().Be("{\"id\":\"completed\"}");
        committer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Rejection_is_terminal_and_does_not_invoke_committer()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", ClinicWorkspaceActionCommitResult.Completed("{}"));
        RejectClinicWorkspaceActionHandler reject = new(store, RegistryFor(committer), new FixedTimeProvider(Now));

        ClinicWorkspaceActionResult result = await reject.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }), "  not requested  "), CancellationToken.None);
        _ = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        result.Action!.Status.Should().Be(ClinicWorkspaceActionStatus.Rejected);
        result.Action.RejectionReason.Should().Be("not requested");
        committer.InvocationCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true, false, ClinicWorkspaceActionOutcome.ScopeMismatch)]
    [InlineData(false, true, ClinicWorkspaceActionOutcome.ScopeMismatch)]
    [InlineData(false, false, ClinicWorkspaceActionOutcome.PermissionDenied)]
    public async Task Confirmation_enforces_patient_practice_and_user_scope(
        bool wrongPatient, bool wrongPractice, ClinicWorkspaceActionOutcome expected)
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        ClinicWorkspaceContext context = Context(
            patientId: wrongPatient ? Guid.NewGuid() : store.Action.PatientId,
            practiceId: wrongPractice ? "other-practice" : store.Action.PracticeId,
            userId: expected == ClinicWorkspaceActionOutcome.PermissionDenied ? "other-user" : store.Action.RequestedByUserId,
            permissions: new HashSet<string> { "Appointments.Create" });

        ClinicWorkspaceActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(store.Action.ActionId, context), CancellationToken.None);

        result.Outcome.Should().Be(expected);
        store.Action.Status.Should().Be(ClinicWorkspaceActionStatus.Proposed);
    }

    [Fact]
    public async Task Missing_capability_permission_is_denied()
    {
        InMemoryActionStore store = new(Action("create_prescription"));
        ClinicWorkspaceActionResult result = await Handler(store, new StubCommitter("create_prescription")).Handle(
            new(store.Action.ActionId, Context()), CancellationToken.None);
        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.PermissionDenied);
    }

    [Fact]
    public async Task Confirmation_rejects_an_action_created_under_an_old_policy_revision()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ActionPolicyRevision = 2 });
        StubCommitter committer = new("create_appointment");

        ClinicWorkspaceActionResult result = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Failed);
        result.Action!.Failure.Should().Be(new ClinicWorkspaceActionFailure(
            "stale_action_policy", "The action policy changed; the action must be proposed again.", false));
        committer.InvocationCount.Should().Be(0);
    }

    [Fact]
    public void Stored_actions_without_operations_scope_fields_remain_patient_scoped()
    {
        JsonObject legacy = JsonSerializer.SerializeToNode(Action("create_appointment"))!.AsObject();
        _ = legacy.Remove(nameof(ClinicWorkspaceAction.SubjectKind));
        _ = legacy.Remove(nameof(ClinicWorkspaceAction.ToolCallId));
        _ = legacy.Remove(nameof(ClinicWorkspaceAction.ToolName));

        ClinicWorkspaceAction restored = legacy.Deserialize<ClinicWorkspaceAction>()!;

        restored.SubjectKind.Should().Be("patient");
        restored.ToolCallId.Should().BeNull();
        restored.ToolName.Should().BeNull();
    }

    [Fact]
    public async Task Practice_action_requires_the_same_owner_and_practice_on_confirmation()
    {
        ClinicWorkspaceAction action = Action("book_appointment") with
        {
            PatientId = Guid.Empty,
            SubjectKind = "practice"
        };
        InMemoryActionStore store = new(action);
        StubCommitter committer = new("book_appointment", ClinicWorkspaceActionCommitResult.Completed("{}"));

        ClinicWorkspaceActionResult nonOwner = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, role: "Clinician")), CancellationToken.None);
        ClinicWorkspaceActionResult wrongPractice = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, practiceId: "other-practice", role: "TenantOwner")), CancellationToken.None);
        ClinicWorkspaceActionResult owner = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, role: "TenantOwner")), CancellationToken.None);

        nonOwner.Outcome.Should().Be(ClinicWorkspaceActionOutcome.PermissionDenied);
        wrongPractice.Outcome.Should().Be(ClinicWorkspaceActionOutcome.ScopeMismatch);
        owner.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Succeeded);
        committer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Unknown_action_is_not_found()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        ClinicWorkspaceActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(Guid.NewGuid(), Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.NotFound);
    }

    [Fact]
    public async Task Expired_action_is_marked_expired_without_committing()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ExpiresAt = Now });
        StubCommitter committer = new("create_appointment");
        ClinicWorkspaceActionResult result = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Expired);
        result.Action!.Status.Should().Be(ClinicWorkspaceActionStatus.Expired);
        committer.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Expired_action_cannot_be_rejected_as_if_it_were_still_reviewable()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ExpiresAt = Now });
        StubCommitter committer = new("create_appointment");
        RejectClinicWorkspaceActionHandler handler = new(store, RegistryFor(committer), new FixedTimeProvider(Now));

        ClinicWorkspaceActionResult result = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }), null), CancellationToken.None);

        result.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Expired);
        result.Action!.Status.Should().Be(ClinicWorkspaceActionStatus.Expired);
    }

    [Fact]
    public async Task Repeated_and_concurrent_confirmation_execute_committer_once()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", ClinicWorkspaceActionCommitResult.Completed("{}"), block: true);
        ConfirmClinicWorkspaceActionHandler handler = Handler(store, committer);
        ConfirmClinicWorkspaceAction command = new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }));

        Task<ClinicWorkspaceActionResult> first = handler.Handle(command, CancellationToken.None);
        await committer.Started.Task;
        ClinicWorkspaceActionResult concurrent = await handler.Handle(command, CancellationToken.None);
        committer.Release.SetResult();
        ClinicWorkspaceActionResult completed = await first;
        ClinicWorkspaceActionResult repeated = await handler.Handle(command, CancellationToken.None);

        concurrent.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Conflict);
        completed.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Succeeded);
        repeated.Outcome.Should().Be(ClinicWorkspaceActionOutcome.Succeeded);
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
        StubCommitter committer = new("create_appointment", ClinicWorkspaceActionCommitResult.Failed(code, "failed", retryable));
        ConfirmClinicWorkspaceActionHandler handler = Handler(store, committer);

        ClinicWorkspaceActionResult first = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        ClinicWorkspaceActionResult second = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        first.Action!.Failure.Should().BeEquivalentTo(new ClinicWorkspaceActionFailure(code, "failed", retryable));
        committer.InvocationCount.Should().Be(retryable ? 2 : 1);
        second.Outcome.Should().Be(retryable ? ClinicWorkspaceActionOutcome.Failed : ClinicWorkspaceActionOutcome.Conflict);
    }

    private static ConfirmClinicWorkspaceActionHandler Handler(InMemoryActionStore store, params StubCommitter[] committers) =>
        new(store, new ClinicWorkspaceActionCommitterRegistry(committers), RegistryFor(committers), new FixedTimeProvider(Now));

    private static ClinicWorkspaceCapabilityRegistry RegistryFor(params StubCommitter[] committers) =>
        new(
            [new TestCapability(committers
                .SelectMany(committer => ((IClinicWorkspaceActionCommitter)committer).ActionTypes)
                .Select(actionType => ClinicWorkspaceActionPolicies.Prepare(
                    actionType,
                    actionType,
                    actionType switch
                    {
                        "create_appointment" => ["Appointments.Create"],
                        "create_prescription" => ["Prescriptions.Create"],
                        "add_clinical_event" => ["ClinicalEvents.Create"],
                        _ => []
                    },
                    requiredRoles: actionType.StartsWith("book_", StringComparison.Ordinal) ? ["TenantOwner"] : [],
                    toolNames: [$"prepare_{actionType}"],
                    retryClassification: ClinicWorkspaceActionRetryClassification.Retryable))
                .ToArray())],
            new ClinicWorkspaceAuthorizationPolicy(),
            new ClinicWorkspaceToolAvailabilityPolicy(),
            committers);

    private sealed record TestCapability(IReadOnlyCollection<ClinicWorkspaceActionPolicy> ActionPolicies)
        : IClinicWorkspaceCapability
    {
        public string Name => "Test";
        public string Description => "Test capability";
        public string GetInstructions() => Description;
        public IReadOnlySet<string> RequiredPermissions => new HashSet<string>();
        public IReadOnlyCollection<IAgentTool> Tools { get; } = ActionPolicies
            .SelectMany(policy => policy.ToolNames.Select(toolName => (IAgentTool)new TestTool(toolName)))
            .ToArray();
        public bool IsAvailable(ClinicWorkspaceContext context) => true;
    }

    private sealed class TestTool(string name) : IAgentTool, IClinicWorkspacePreparationTool
    {
        public AgentToolDefinition Definition { get; } = new(name, name, "{\"type\":\"object\"}");

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));

        public ClinicWorkspacePreparedAction? GetPreparedAction(AgentToolResult result) => null;
    }

    private static ClinicWorkspaceAction Action(string actionType) => new(
        Guid.NewGuid(), "run-1", "conversation-1", DefaultPatientId, "practice-1", "user-1",
        "Capability", actionType, Guid.NewGuid(), "{}", ClinicWorkspaceActionStatus.Proposed,
        Now.AddMinutes(-1), Now.AddMinutes(30));

    private static ClinicWorkspaceContext Context(
        Guid? patientId = null,
        string practiceId = "practice-1",
        string userId = "user-1",
        IReadOnlySet<string>? permissions = null,
        string role = "Clinician") =>
        new(practiceId, patientId ?? DefaultPatientId, userId, permissions ?? new HashSet<string>(),
            role, "en-US", "UTC", "conversation-1", SubjectKind: patientId == Guid.Empty ? "practice" : "patient");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubCommitter(
        string actionType,
        ClinicWorkspaceActionCommitResult? result = null,
        bool block = false) : IClinicWorkspaceActionCommitter
    {
        public string ActionType => actionType;
        public int InvocationCount { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(
            ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
        {
            InvocationCount++;
            Started.TrySetResult();
            if (block) await Release.Task.WaitAsync(cancellationToken);
            return result ?? ClinicWorkspaceActionCommitResult.Completed("{}");
        }
    }

    private sealed class InMemoryActionStore(ClinicWorkspaceAction action) : IClinicWorkspaceActionStore
    {
        private readonly object _sync = new();
        public ClinicWorkspaceAction Action { get; private set; } = action;

        public Task SaveAsync(ClinicWorkspaceAction value, CancellationToken cancellationToken)
        {
            lock (_sync) Action = value;
            return Task.CompletedTask;
        }

        public Task<ClinicWorkspaceAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
        {
            lock (_sync) return Task.FromResult<ClinicWorkspaceAction?>(Action.ActionId == actionId ? Action : null);
        }

        public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == ClinicWorkspaceActionStatus.Proposed ||
                retryFailed && current.Status == ClinicWorkspaceActionStatus.Failed && current.Failure?.Retryable == true,
                current => current with { Status = ClinicWorkspaceActionStatus.Confirming, Failure = null });

        public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == ClinicWorkspaceActionStatus.Proposed,
                current => current with { Status = ClinicWorkspaceActionStatus.Rejected, RejectedAt = rejectedAt, RejectionReason = reason });

        public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status == ClinicWorkspaceActionStatus.Confirming,
                current => current with { Status = ClinicWorkspaceActionStatus.Completed, ConfirmedAt = confirmedAt, ConfirmedByUserId = confirmedByUserId, CompletionPayload = completionPayload });

        public Task<bool> FailAsync(Guid actionId, ClinicWorkspaceActionFailure failure, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status is ClinicWorkspaceActionStatus.Confirming or ClinicWorkspaceActionStatus.Proposed,
                current => current with { Status = ClinicWorkspaceActionStatus.Failed, Failure = failure });

        public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) =>
            Transition(actionId, current => current.Status is ClinicWorkspaceActionStatus.Proposed or ClinicWorkspaceActionStatus.Failed,
                current => current with { Status = ClinicWorkspaceActionStatus.Expired });

        private Task<bool> Transition(Guid actionId, Func<ClinicWorkspaceAction, bool> predicate, Func<ClinicWorkspaceAction, ClinicWorkspaceAction> update)
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
