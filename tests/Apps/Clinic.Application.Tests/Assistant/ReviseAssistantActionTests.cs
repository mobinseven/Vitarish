using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;
using Xunit;
using AssistantActionStatus = Vitarish.Clinic.Application.Assistant.AssistantActionStatus;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class ReviseAssistantActionTests
{
    private const string RevisableType = "create_appointment";
    private const string RevisableTool = "prepare_create_appointment";
    private const string FixedType = "create_prescription";
    private const string OwnerOnlyType = "book_appointment";
    private const string CapabilityName = "Capability";
    private const string OriginalPayload = "{\"operation\":\"create\",\"note\":\"before\"}";
    private const string RevisedArguments = "{\"operation\":\"create\",\"note\":\"after\"}";

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-01T12:00:00Z");
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OriginalId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly FixedTimeProvider _time = new(Now);
    private readonly InMemoryActionStore _store = new();
    private readonly RecordingPreparation _preparation = new();

    private ReviseAssistantActionHandler Handler()
    {
        StubCommitter[] committers = [new(RevisableType), new(FixedType), new(OwnerOnlyType)];
        AssistantCapabilityRegistry registry = new(
            [new TestCapability(
            [
                AssistantActionPolicies.Prepare(
                    RevisableType, RevisableType, ["Appointments.Create"], toolNames: [RevisableTool], revisable: true),
                AssistantActionPolicies.Prepare(
                    FixedType, FixedType, ["Prescriptions.Create"], toolNames: ["prepare_create_prescription"]),
                AssistantActionPolicies.Prepare(
                    OwnerOnlyType, OwnerOnlyType, [], requiredRoles: ["TenantOwner"], toolNames: ["prepare_book_appointment"], revisable: true)
            ])],
            new AssistantAuthorizationPolicy(),
            new AssistantToolAvailabilityPolicy(),
            committers);
        return new(
            new PreparedActionLifecycle(_store, _time),
            new AssistantActionCommitterRegistry(committers),
            registry,
            _preparation,
            _time);
    }

    private static PreparedAction Seed(
        string actionType = RevisableType,
        string? toolName = RevisableTool,
        string payload = OriginalPayload) => new(
        OriginalId, AssistantSubjects.PatientAgentKey, "practice-1", AssistantSubjects.PatientKind, PatientId.ToString("D"),
        "conversation-1", "run-1", "user-1", CapabilityName, actionType, 1, payload, "original summary", "tool-call-1",
        toolName, PreparedActionStatus.Proposed, Now.AddMinutes(-1), Now.AddMinutes(30));

    private static AssistantContext Context(string role = "Clinician", string[]? permissions = null) =>
        new("practice-1", PatientId, "user-1", new HashSet<string>(permissions ?? ["Appointments.Create", "Prescriptions.Create"]),
            role, "en-US", "UTC", "conversation-1", SubjectKind: AssistantSubjects.PatientKind);

    private Task<AssistantActionResult> Revise(string arguments = RevisedArguments, AssistantContext? context = null) =>
        Handler().Handle(new(OriginalId, context ?? Context(), arguments), CancellationToken.None);

    private void AssertOriginalUntouched(PreparedAction original)
    {
        _store.Current(OriginalId).Should().Be(original);
        _store.Current(OriginalId)!.Status.Should().Be(PreparedActionStatus.Proposed);
        _store.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task A_revision_replaces_the_proposal_in_the_same_run_and_conversation()
    {
        _store.Seed(Seed());

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        AssistantAction replacement = result.Action!;
        replacement.ActionId.Should().NotBe(OriginalId);
        replacement.Status.Should().Be(AssistantActionStatus.Proposed);
        replacement.AgentRunId.Should().Be("run-1");
        replacement.ConversationId.Should().Be("conversation-1");
        replacement.ToolName.Should().Be(RevisableTool);
        replacement.Summary.Should().Be("original summary");
        replacement.ReviewPayload.Should().Be(RevisedArguments);
        replacement.ActionType.Should().Be(RevisableType);
        replacement.Capability.Should().Be(CapabilityName);
        _store.Current(replacement.ActionId)!.Status.Should().Be(PreparedActionStatus.Proposed);
        PreparedAction original = _store.Current(OriginalId)!;
        original.Status.Should().Be(PreparedActionStatus.Rejected);
        original.RejectionReason.Should().Be("revised");
        _preparation.Calls.Should().ContainSingle().Which.Should().Be((RevisableTool, RevisedArguments));
    }

    [Fact]
    public async Task A_policy_that_is_not_revisable_is_a_validation_failure()
    {
        PreparedAction original = Seed(FixedType, "prepare_create_prescription");
        _store.Seed(original);

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.ValidationFailure);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("prepare_create_prescription")]
    [InlineData(null)]
    public async Task A_stored_tool_the_policy_does_not_list_is_a_validation_failure(string? toolName)
    {
        PreparedAction original = Seed(toolName: toolName);
        _store.Seed(original);

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.ValidationFailure);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{\"operation\":\"delete\"}")]
    [InlineData("{\"note\":\"no operation\"}")]
    [InlineData("{\"operation\":1}")]
    [InlineData("[]")]
    [InlineData("not json")]
    public async Task Revised_arguments_that_change_or_drop_the_operation_are_a_validation_failure(string arguments)
    {
        PreparedAction original = Seed();
        _store.Seed(original);

        AssistantActionResult result = await Revise(arguments);

        result.Outcome.Should().Be(AssistantActionOutcome.ValidationFailure);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task An_original_payload_without_an_operation_accepts_any_object_arguments()
    {
        _store.Seed(Seed(payload: "{\"note\":\"before\"}"));

        AssistantActionResult result = await Revise("{\"note\":\"after\"}");

        result.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
    }

    [Fact]
    public async Task A_preparation_that_returns_nothing_is_a_validation_failure()
    {
        PreparedAction original = Seed();
        _store.Seed(original);
        _preparation.Result = _ => null;

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.ValidationFailure);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("other-type", CapabilityName)]
    [InlineData(RevisableType, "OtherCapability")]
    public async Task A_prepared_action_of_a_different_type_or_capability_is_a_validation_failure(string actionType, string capability)
    {
        PreparedAction original = Seed();
        _store.Seed(original);
        _preparation.Result = args => new(capability, actionType, Guid.NewGuid(), args, null);

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.ValidationFailure);
        AssertOriginalUntouched(original);
    }

    [Fact]
    public async Task A_staff_user_without_the_required_role_is_permission_denied_and_nothing_is_prepared()
    {
        PreparedAction original = Seed(OwnerOnlyType, "prepare_book_appointment");
        _store.Seed(original);

        AssistantActionResult result = await Revise(context: Context(role: "Clinician"));

        result.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task A_tenant_owner_can_revise_an_owner_only_action()
    {
        _store.Seed(Seed(OwnerOnlyType, "prepare_book_appointment"));
        _preparation.Result = args => new(CapabilityName, OwnerOnlyType, Guid.NewGuid(), args, null);

        AssistantActionResult result = await Revise(context: Context(role: "TenantOwner"));

        result.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        _preparation.Calls.Should().HaveCount(1);
    }

    [Fact]
    public async Task A_caller_without_the_capability_permission_is_permission_denied()
    {
        PreparedAction original = Seed();
        _store.Seed(original);

        AssistantActionResult result = await Revise(context: Context(permissions: []));

        result.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
        AssertOriginalUntouched(original);
        _preparation.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Revising_an_unknown_action_is_not_found()
    {
        AssistantActionResult result = await Handler().Handle(new(Guid.NewGuid(), Context(), RevisedArguments), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.NotFound);
        _preparation.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Revising_an_expired_action_is_expired()
    {
        _store.Seed(Seed() with { ExpiresAt = Now });

        AssistantActionResult result = await Revise();

        result.Outcome.Should().Be(AssistantActionOutcome.Expired);
        _preparation.Calls.Should().BeEmpty();
    }

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

    private sealed class StubCommitter(string actionType) : IAssistantActionCommitter
    {
        public string ActionType => actionType;

        public Task<AssistantActionCommitResult> CommitAsync(
            AssistantAction action, AssistantContext context, CancellationToken cancellationToken) =>
            Task.FromResult(AssistantActionCommitResult.Completed("{}"));
    }

    private sealed class RecordingPreparation : IAssistantActionPreparation
    {
        public List<(string ToolName, string ArgumentsJson)> Calls { get; } = [];

        public Func<string, AssistantPreparedAction?> Result { get; set; } = args =>
            new(CapabilityName, RevisableType, Guid.NewGuid(), args, null, "model summary", "model-call", "model-tool");

        public Task<AssistantPreparedAction?> PrepareAsync(
            AssistantContext context, string toolName, string argumentsJson, CancellationToken cancellationToken = default)
        {
            Calls.Add((toolName, argumentsJson));
            return Task.FromResult(Result(argumentsJson));
        }
    }

    private sealed class InMemoryActionStore : IPreparedActionStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<Guid, StoredPreparedAction> _actions = [];
        private long _version;

        public int SaveCount { get; private set; }

        public void Seed(PreparedAction action)
        {
            lock (_sync) _actions[action.ActionId] = new(action, NextVersion());
        }

        public PreparedAction? Current(Guid actionId)
        {
            lock (_sync) return _actions.TryGetValue(actionId, out StoredPreparedAction? stored) ? stored.Action : null;
        }

        public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            lock (_sync)
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
            lock (_sync) return Task.FromResult(_actions.GetValueOrDefault(actionId));
        }

        public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (!_actions.TryGetValue(current.Action.ActionId, out StoredPreparedAction? existing) ||
                    existing.Version != current.Version)
                    return Task.FromResult(false);
                _actions[current.Action.ActionId] = new(next, NextVersion());
                return Task.FromResult(true);
            }
        }

        private string NextVersion() => (++_version).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
