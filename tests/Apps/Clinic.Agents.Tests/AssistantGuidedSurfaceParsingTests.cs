using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests;

public sealed class AssistantGuidedSurfaceParsingTests
{
    private const string PreparedMessage = "Prepared";
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Operations_result_schema_adds_a_question_and_a_next_action_limited_to_setup_goals()
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = Compose(PracticeContext());

        JsonNode schema = JsonNode.Parse(manifest.Definition.Result!.JsonSchema)!;
        JsonObject properties = schema["properties"]!.AsObject();
        properties.ContainsKey(AgentInteractiveReplyFormat.QuestionProperty).Should().BeTrue();
        properties.ContainsKey(AgentInteractiveReplyFormat.NextActionProperty).Should().BeTrue();
        schema["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Contain([AgentInteractiveReplyFormat.QuestionProperty, AgentInteractiveReplyFormat.NextActionProperty]);

        JsonArray allowed = properties[AgentInteractiveReplyFormat.NextActionProperty]!["enum"]!.AsArray();
        allowed.Where(node => node is not null).Select(node => node!.GetValue<string>())
            .Should().BeEquivalentTo(Enum.GetNames<ClinicSetupGoal>());
        allowed.Should().HaveCount(Enum.GetNames<ClinicSetupGoal>().Length + 1);
        allowed.Count(node => node is null).Should().Be(1);
        properties[AgentInteractiveReplyFormat.QuestionProperty]!["type"]!.AsArray()
            .Select(node => node!.GetValue<string>()).Should().Equal("object", "null");
    }

    [Fact]
    public void Patient_result_schema_is_unchanged()
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = Compose(PatientContext());

        manifest.Definition.Result!.JsonSchema.Should().Be(AssistantRuntime.ResultSchema);
        AssistantRuntime.ResultSchema.Should().NotContain(AgentInteractiveReplyFormat.NextActionProperty);
    }

    [Fact]
    public async Task A_malformed_question_and_unknown_next_action_are_dropped_without_losing_the_reply_or_actions()
    {
        Guid proposalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        RecordingActionStore actionStore = new();
        AssistantRuntime runtime = Runtime(
            new RecordingSender(
                """{"prompt":"Which?","options":[{"key":"a","label":"Only one"}],"maxSelections":1}""",
                "\"NotAGoal\""),
            actionStore,
            [new TestCapability("Operations", [new TestPreparationTool(proposalId)], ["create_appointment"])]);

        AssistantResult result = await runtime.RunAsync(PracticeContext(), new AssistantInput("prepare"), CancellationToken.None);

        result.Messages.Should().ContainSingle().Which.Content.Should().Be(PreparedMessage);
        result.Actions.Should().ContainSingle().Which.ActionId.Should().Be(proposalId);
        actionStore.Saved.Should().ContainSingle().Which.ActionId.Should().Be(proposalId);
        result.Question.Should().BeNull();
        result.NextAction.Should().BeNull();
    }

    [Fact]
    public async Task A_valid_question_and_next_action_are_carried_on_the_result()
    {
        AssistantRuntime runtime = Runtime(
            new RecordingSender(
                """{"prompt":"Which area first?","options":[{"key":"staff","label":"Staff"},{"key":"services","label":"Services"}],"maxSelections":1}""",
                "\"StaffUsers\""),
            new RecordingActionStore(),
            [new TestCapability("Operations", [new TestPreparationTool(Guid.NewGuid())], ["create_appointment"])]);

        AssistantResult result = await runtime.RunAsync(PracticeContext(), new AssistantInput("help"), CancellationToken.None);

        result.Question.Should().NotBeNull();
        result.Question!.Prompt.Should().Be("Which area first?");
        result.Question.Options.Select(option => option.Key).Should().Equal("staff", "services");
        result.Question.MaxSelections.Should().Be(1);
        result.NextAction.Should().Be(nameof(ClinicSetupGoal.StaffUsers));
    }

    private static ComposedAgentManifest<AssistantCompositionContext> Compose(AssistantContext context)
    {
        AssistantCompositionContext composition = new(context, [], DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        AgentProfile<AssistantCompositionContext> profile = context.SubjectKind == AssistantSubjects.PracticeKind
            ? new ClinicOperationsAssistantProfile(composition, new("test-model"))
            : new ClinicPatientAssistantProfile(composition, new("test-model"));
        return Composer([]).Compose(profile, composition, _ => new AgentExposureDecision(true, "enabled"));
    }

    private static AgentProfileComposer<AssistantCompositionContext> Composer(
        IReadOnlyCollection<IAssistantCapability> capabilities) =>
        new(new AgentCapabilityCatalog<AssistantCompositionContext>(
            AssistantCapabilityKeys.Patient
                .Concat(AssistantCapabilityKeys.OperationsProfile)
                .Concat(capabilities.Select(capability => capability.Name))
                .Distinct(StringComparer.Ordinal)
                .Select(key => (IAgentCapabilityPack<AssistantCompositionContext>)new AssistantCapabilityPack(key))));

    private static AssistantRuntime Runtime(
        RecordingSender sender,
        RecordingActionStore actionStore,
        IReadOnlyCollection<IAssistantCapability> capabilities)
    {
        AssistantCapabilityRegistry registry = new(
            capabilities,
            new AssistantAuthorizationPolicy(),
            new AssistantToolAvailabilityPolicy(),
            capabilities
                .SelectMany(capability => capability.ActionPolicies)
                .Select(policy => new TestCommitter(policy.ActionType, policy.OwningWorkflow))
                .ToArray());
        FixedTimeProvider time = new(DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        AssistantManifestComposer manifestComposer = new(
            Composer(capabilities),
            registry,
            new AssistantToolDispatcher(registry),
            new AssistantOptions("test-model"),
            new AssistantExecutionContextFactory(time));
        return new(
            new AssistantConversationRunner(
                manifestComposer,
                new AssistantResultParser(registry),
                sender,
                new PreparedActionLifecycle(actionStore, time),
                registry,
                time),
            new AssistantToolResolver(manifestComposer),
            manifestComposer);
    }

    private static AssistantContext PatientContext() => new(
        "practice-1", PatientId, "user-1", new HashSet<string>(), "Clinician", "en-US", "UTC", "conversation-1");

    private static AssistantContext PracticeContext() => new(
        "practice-1", Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner", "en-US", "UTC", "conversation-1",
        SubjectKind: AssistantSubjects.PracticeKind);

    private sealed record TestCapability(
        string Name,
        IReadOnlyCollection<IAgentTool> Tools,
        IReadOnlyCollection<string> Actions) : IAssistantCapability
    {
        public string Description => $"{Name} test capability";
        public string GetInstructions() => $"Use {Name}.";
        public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>();
        public IReadOnlyCollection<AssistantActionPolicy> ActionPolicies { get; } = Actions.Select(action => AssistantActionPolicies.Prepare(
            action,
            Name,
            [],
            toolNames: Tools.Select(tool => tool.Definition.Name))).ToArray();
        public IReadOnlyCollection<string> ActionTypes => ActionPolicies.Select(policy => policy.ActionType).ToArray();
        public bool IsAvailable(AssistantContext context) => true;
    }

    private sealed class TestCommitter(string actionType, string workflowKey) : IAssistantActionCommitter
    {
        public string ActionType => actionType;
        public string WorkflowKey => workflowKey;

        public Task<AssistantActionCommitResult> CommitAsync(
            AssistantAction action,
            AssistantContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(AssistantActionCommitResult.Completed("{}"));
    }

    private sealed class TestPreparationTool(Guid proposalId) : IAgentTool, IAssistantPreparationTool
    {
        public AgentToolDefinition Definition { get; } = new("prepare_appointment", "prepare_appointment", "{\"type\":\"object\",\"additionalProperties\":false}");

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));

        public AssistantPreparedAction? GetPreparedAction(AgentToolResult result) =>
            result.IsError ? null : new(
                "Operations", "create_appointment", proposalId, "{\"proposal\":true}",
                DateTimeOffset.Parse("2030-01-01T11:00:00Z"));
    }

    private sealed class RecordingSender(string questionJson, string nextActionJson) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            RunAgentConversation conversation = request as RunAgentConversation
                ?? throw new InvalidOperationException("Unexpected request.");
            JsonElement output = JsonDocument.Parse(
                "{\"messages\":[{\"kind\":\"assistantMessage\",\"content\":\"" + PreparedMessage + "\"}],"
                + "\"actions\":[{\"capability\":\"Operations\",\"actionType\":\"create_appointment\",\"summary\":\"Appointment\"}],"
                + "\"question\":" + questionJson + ",\"nextAction\":" + nextActionJson + "}"
            ).RootElement.Clone();
            AgentRunResult run = new(true, null, output, new(0, 0), TimeSpan.Zero, 1, 1)
            {
                History = "history",
                ToolExecutions = [new("prepare_appointment", "call-1", "prepared")]
            };
            Guid conversationId = Guid.Parse("018f47c2-a6b7-7000-8000-000000000001");
            AgentConversationOutcome outcome = conversation.BeforeCommit is null
                ? AgentConversationOutcome.Succeeded
                : await conversation.BeforeCommit(conversationId, run, cancellationToken);
            return (TResponse)(object)new RunAgentConversationResult(outcome, conversationId, run);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task ISender.Send<TRequest>(TRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingActionStore : IPreparedActionStore
    {
        public List<PreparedAction> Saved { get; } = [];

        public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            Saved.Add(action);
            return Task.CompletedTask;
        }

        public Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.SingleOrDefault(action => action.ActionId == actionId) is { } action
                ? new StoredPreparedAction(action, "0")
                : null);

        public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
