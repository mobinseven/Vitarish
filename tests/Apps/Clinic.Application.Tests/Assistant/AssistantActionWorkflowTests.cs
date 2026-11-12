using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;
using Xunit;
using AssistantActionStatus = Vitarish.Clinic.Application.Assistant.AssistantActionStatus;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class AssistantActionWorkflowTests
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
        StubCommitter committer = new(actionType, AssistantActionCommitResult.Completed("{\"id\":\"completed\"}"));
        ConfirmAssistantActionHandler handler = Handler(store, committer);

        AssistantActionResult result = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { permission })), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        result.Action!.Status.Should().Be(AssistantActionStatus.Completed);
        result.Action.CompletionPayload.Should().Be("{\"id\":\"completed\"}");
        committer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Rejection_is_terminal_and_does_not_invoke_committer()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", AssistantActionCommitResult.Completed("{}"));
        RejectAssistantActionHandler reject = new(Lifecycle(store), new AssistantActionCommitterRegistry([committer]), RegistryFor(committer));

        AssistantActionResult result = await reject.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }), "  not requested  "), CancellationToken.None);
        _ = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        result.Action!.Status.Should().Be(AssistantActionStatus.Rejected);
        result.Action.RejectionReason.Should().Be("not requested");
        committer.InvocationCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true, false, AssistantActionOutcome.ScopeMismatch)]
    [InlineData(false, true, AssistantActionOutcome.ScopeMismatch)]
    [InlineData(false, false, AssistantActionOutcome.PermissionDenied)]
    public async Task Confirmation_enforces_patient_practice_and_user_scope(
        bool wrongPatient, bool wrongPractice, AssistantActionOutcome expected)
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        AssistantContext context = Context(
            patientId: wrongPatient ? Guid.NewGuid() : store.Action.PatientId,
            practiceId: wrongPractice ? "other-practice" : store.Action.PracticeId,
            userId: expected == AssistantActionOutcome.PermissionDenied ? "other-user" : store.Action.RequestedByUserId,
            permissions: new HashSet<string> { "Appointments.Create" });

        AssistantActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(store.Action.ActionId, context), CancellationToken.None);

        result.Outcome.Should().Be(expected);
        store.Action.Status.Should().Be(AssistantActionStatus.Proposed);
    }

    [Fact]
    public async Task Missing_capability_permission_is_denied()
    {
        InMemoryActionStore store = new(Action("create_prescription"));
        AssistantActionResult result = await Handler(store, new StubCommitter("create_prescription")).Handle(
            new(store.Action.ActionId, Context()), CancellationToken.None);
        result.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
    }

    [Fact]
    public async Task Confirmation_rejects_an_action_created_under_an_old_policy_revision()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ActionPolicyRevision = 2 });
        StubCommitter committer = new("create_appointment");

        AssistantActionResult result = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.Failed);
        result.Action!.Failure.Should().Be(new AssistantActionFailure(
            "stale_action_policy", "The action policy changed; the action must be proposed again.", false));
        committer.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Practice_action_requires_the_same_owner_and_practice_on_confirmation()
    {
        AssistantAction action = Action("book_appointment") with
        {
            PatientId = Guid.Empty,
            SubjectKind = "practice"
        };
        InMemoryActionStore store = new(action);
        StubCommitter committer = new("book_appointment", AssistantActionCommitResult.Completed("{}"));

        AssistantActionResult nonOwner = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, role: "Clinician")), CancellationToken.None);
        AssistantActionResult wrongPractice = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, practiceId: "other-practice", role: "TenantOwner")), CancellationToken.None);
        AssistantActionResult owner = await Handler(store, committer).Handle(
            new(action.ActionId, Context(patientId: Guid.Empty, role: "TenantOwner")), CancellationToken.None);

        nonOwner.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
        wrongPractice.Outcome.Should().Be(AssistantActionOutcome.ScopeMismatch);
        owner.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        committer.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Unknown_action_is_not_found()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        AssistantActionResult result = await Handler(store, new StubCommitter("create_appointment")).Handle(
            new(Guid.NewGuid(), Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(AssistantActionOutcome.NotFound);
    }

    [Fact]
    public async Task Expired_action_is_marked_expired_without_committing()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ExpiresAt = Now });
        StubCommitter committer = new("create_appointment");
        AssistantActionResult result = await Handler(store, committer).Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        result.Outcome.Should().Be(AssistantActionOutcome.Expired);
        result.Action!.Status.Should().Be(AssistantActionStatus.Expired);
        committer.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Expired_action_cannot_be_rejected_as_if_it_were_still_reviewable()
    {
        InMemoryActionStore store = new(Action("create_appointment") with { ExpiresAt = Now });
        StubCommitter committer = new("create_appointment");
        RejectAssistantActionHandler handler = new(Lifecycle(store), new AssistantActionCommitterRegistry([committer]), RegistryFor(committer));

        AssistantActionResult result = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }), null), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.Expired);
        result.Action!.Status.Should().Be(AssistantActionStatus.Expired);
    }

    [Fact]
    public async Task Repeated_and_concurrent_confirmation_execute_committer_once()
    {
        InMemoryActionStore store = new(Action("create_appointment"));
        StubCommitter committer = new("create_appointment", AssistantActionCommitResult.Completed("{}"), block: true);
        ConfirmAssistantActionHandler handler = Handler(store, committer);
        ConfirmAssistantAction command = new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" }));

        Task<AssistantActionResult> first = handler.Handle(command, CancellationToken.None);
        await committer.Started.Task;
        AssistantActionResult concurrent = await handler.Handle(command, CancellationToken.None);
        committer.Release.SetResult();
        AssistantActionResult completed = await first;
        AssistantActionResult repeated = await handler.Handle(command, CancellationToken.None);

        concurrent.Outcome.Should().Be(AssistantActionOutcome.Conflict);
        completed.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        repeated.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
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
        StubCommitter committer = new("create_appointment", AssistantActionCommitResult.Failed(code, "failed", retryable));
        ConfirmAssistantActionHandler handler = Handler(store, committer);

        AssistantActionResult first = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);
        AssistantActionResult second = await handler.Handle(
            new(store.Action.ActionId, Context(permissions: new HashSet<string> { "Appointments.Create" })), CancellationToken.None);

        first.Action!.Failure.Should().BeEquivalentTo(new AssistantActionFailure(code, "failed", retryable));
        committer.InvocationCount.Should().Be(retryable ? 2 : 1);
        second.Outcome.Should().Be(retryable ? AssistantActionOutcome.Failed : AssistantActionOutcome.Conflict);
    }

    private static ConfirmAssistantActionHandler Handler(InMemoryActionStore store, params StubCommitter[] committers) =>
        new(Lifecycle(store), new AssistantActionCommitterRegistry(committers), RegistryFor(committers));

    private static PreparedActionLifecycle Lifecycle(InMemoryActionStore store) => new(store, new FixedTimeProvider(Now));

    private static AssistantCapabilityRegistry RegistryFor(params StubCommitter[] committers) =>
        new(
            [new TestCapability(committers
                .SelectMany(committer => ((IAssistantActionCommitter)committer).ActionTypes)
                .Select(actionType => AssistantActionPolicies.Prepare(
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
                    retryClassification: AssistantActionRetryClassification.Retryable))
                .ToArray())],
            new AssistantAuthorizationPolicy(),
            new AssistantToolAvailabilityPolicy(),
            committers);

    private sealed record TestCapability(IReadOnlyCollection<AssistantActionPolicy> ActionPolicies)
        : IAssistantCapability
    {
        public string Name => "Test";
        public string Description => "Test capability";
        public string GetInstructions() => Description;
        public IReadOnlySet<string> RequiredPermissions => new HashSet<string>();
        public IReadOnlyCollection<IAgentTool> Tools { get; } = ActionPolicies
            .SelectMany(policy => policy.ToolNames.Select(toolName => (IAgentTool)new TestTool(toolName)))
            .ToArray();
        public bool IsAvailable(AssistantContext context) => true;
    }

    private sealed class TestTool(string name) : IAgentTool, IAssistantPreparationTool
    {
        public AgentToolDefinition Definition { get; } = new(name, name, "{\"type\":\"object\"}");

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));

        public AssistantPreparedAction? GetPreparedAction(AgentToolResult result) => null;
    }

    private static AssistantAction Action(string actionType) => new(
        Guid.NewGuid(), "run-1", "conversation-1", DefaultPatientId, "practice-1", "user-1",
        "Capability", actionType, Guid.NewGuid(), "{}", AssistantActionStatus.Proposed,
        Now.AddMinutes(-1), Now.AddMinutes(30), ActionPolicyRevision: 1);

    private static AssistantContext Context(
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
        AssistantActionCommitResult? result = null,
        bool block = false) : IAssistantActionCommitter
    {
        public string ActionType => actionType;
        public int InvocationCount { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<AssistantActionCommitResult> CommitAsync(
            AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
        {
            InvocationCount++;
            Started.TrySetResult();
            if (block) await Release.Task.WaitAsync(cancellationToken);
            return result ?? AssistantActionCommitResult.Completed("{}");
        }
    }

    private sealed class InMemoryActionStore(AssistantAction seed) : IPreparedActionStore
    {
        private readonly object _sync = new();
        private PreparedAction _current = Prepared(seed);
        private long _version;

        public AssistantAction Action
        {
            get { lock (_sync) return ClinicPreparedActionMapping.ToClinic(_current); }
        }

        public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The workflow tests start from a stored action.");

        public Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
        {
            lock (_sync)
                return Task.FromResult(_current.ActionId == actionId
                    ? new StoredPreparedAction(_current, _version.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    : null);
        }

        public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_current.ActionId != next.ActionId ||
                    current.Version != _version.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    return Task.FromResult(false);
                _current = next;
                _version++;
                return Task.FromResult(true);
            }
        }

        private static PreparedAction Prepared(AssistantAction action) => new(
            action.ActionId,
            action.SubjectKind == AssistantSubjects.PracticeKind ? AssistantSubjects.OperationsAgentKey : AssistantSubjects.PatientAgentKey,
            action.PracticeId,
            action.SubjectKind,
            action.SubjectKind == AssistantSubjects.PracticeKind ? action.PracticeId : action.PatientId.ToString("D"),
            action.ConversationId,
            action.AgentRunId,
            action.RequestedByUserId,
            action.Capability,
            action.ActionType,
            action.ActionPolicyRevision,
            action.ReviewPayload,
            action.Summary,
            action.ToolCallId,
            action.ToolName,
            PreparedActionStatus.Proposed,
            action.PreparedAt,
            action.ExpiresAt);
    }
}
