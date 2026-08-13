using System.Text.Json;
using FluentAssertions;
using MediatR;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests;

public sealed class ClinicAssistantSecurityRegressionTests
{
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Patient_scope_is_server_bound_even_when_user_input_requests_another_patient()
    {
        ClinicWorkspaceContext context = PatientContext();
        ClinicAssistantCompositionContext composition = new(context, [], [], DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        AgentProfileComposer<ClinicAssistantCompositionContext> composer = Composer([]);
        ComposedAgentManifest<ClinicAssistantCompositionContext> manifest = composer.Compose(
            new ClinicPatientAssistantProfile(composition, new("test-model")), composition,
            _ => new AgentExposureDecision(true, "enabled"));

        manifest.Definition.Instructions.Should()
            .Contain("opaque server-bound reference")
            .And.Contain("model or user input cannot change or override it")
            .And.NotContain(PatientId.ToString("D"));
    }

    [Fact]
    public void Capabilities_without_all_required_permissions_are_not_composed()
    {
        TestCapability readable = new("Readable", ["Patient.Read"], [new TestTool("read_patient")]);
        TestCapability billing = new("Billing", ["Billing.Read"], [new TestTool("read_billing")]);
        ClinicWorkspaceCapabilityRegistry registry = new(
            [readable, billing],
            new ClinicWorkspaceAuthorizationPolicy(),
            new ClinicWorkspaceToolAvailabilityPolicy(),
            []);

        registry.GetAvailable(PatientContext(new HashSet<string> { "Patient.Read" }))
            .Select(capability => capability.Name)
            .Should().Equal("Readable");
    }

    [Fact]
    public void PlatformAdmin_bypasses_action_permissions_consistently_with_capabilities()
    {
        ClinicWorkspaceActionPolicy policy = ClinicWorkspaceActionPolicies.Direct(
            "read_sensitive_workspace", "operations", ["Workspace.Sensitive.Read"]);

        policy.Authorizes(PatientContext(role: "PlatformAdmin")).Should().BeTrue();
    }

    [Fact]
    public async Task Action_policy_authorization_controls_mutation_exposure_and_dispatch()
    {
        TestPreparationTool preparation = new(Guid.NewGuid());
        TestCapability capability = new(
            "Appointments",
            ["Appointments.Read"],
            [preparation],
            ActionPoliciesOverride:
            [ClinicWorkspaceActionPolicies.Prepare(
                "create_appointment", "appointments", ["Appointments.Create"], toolNames: [preparation.Definition.Name])]);
        ClinicWorkspaceContext context = PatientContext(new HashSet<string> { "Appointments.Read" });
        ClinicAssistantCompositionContext composition = new(context, [capability], [], DateTimeOffset.Parse("2030-01-01T10:00:00Z"));

        AgentCapabilityContribution contribution = new ClinicAssistantCapabilityPack("Appointments").CreateContribution(composition);

        contribution.Tools.Should().BeEmpty();
        contribution.ActionTypes.Should().BeEmpty();
        ClinicAssistantPolicyInstructions.Create(composition, dynamicTime: false)
            .Should().NotContain("create_appointment");

        ClinicWorkspaceCapabilityRegistry registry = new(
            [capability],
            new ClinicWorkspaceAuthorizationPolicy(),
            new ClinicWorkspaceToolAvailabilityPolicy(),
            [new TestCommitter("create_appointment", "appointments")]);
        IAgentTool guarded = new ClinicWorkspaceToolDispatcher(registry, [])
            .CreateGuardedTools(context, Execution(context, "run-1"), [preparation], new Dictionary<string, string> { [preparation.Definition.Name] = capability.Name })
            .Single();

        await FluentActions.Invoking(() => guarded.ExecuteAsync(new AgentToolCall("call-1", preparation.Definition.Name, "{}"), Execution(context, "run-1")))
            .Should().ThrowAsync<UnauthorizedAccessException>();
        preparation.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public async Task Patient_tool_dispatch_rejects_a_callback_for_another_patient()
    {
        ClinicWorkspaceContext context = PatientContext();
        AgentExecutionContext trustedExecution = Execution(context, "run-1");
        TestTool handler = new("patient_read");
        IAgentTool guarded = new ClinicWorkspaceToolDispatcher(TestRegistry(), [])
            .CreateGuardedTools(
                context,
                trustedExecution,
                [handler],
                new Dictionary<string, string> { [handler.Definition.Name] = "Readable" })
            .Single();

        Func<Task> execute = () => guarded.ExecuteAsync(
            new AgentToolCall("call-1", "patient_read", "{}"),
            trustedExecution with
            {
                Metadata = new Dictionary<string, string>(trustedExecution.Metadata!)
                {
                    ["patientId"] = Guid.NewGuid().ToString("D")
                }
            });

        await execute.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not correlated*");
        handler.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public async Task Tool_dispatch_validates_declared_schema_before_execution()
    {
        ClinicWorkspaceContext context = PatientContext();
        AgentExecutionContext execution = Execution(context, "run-1");
        TestTool handler = new(
            "appointments_list",
            """{"type":"object","additionalProperties":false,"required":["range"],"properties":{"range":{"type":"string"}}}""");
        IAgentTool guarded = new ClinicWorkspaceToolDispatcher(TestRegistry(), [])
            .CreateGuardedTools(
                context, execution, [handler],
                new Dictionary<string, string> { [handler.Definition.Name] = "Appointments" })
            .Single();

        Func<Task> invalid = () => guarded.ExecuteAsync(
            new AgentToolCall("call-1", "appointments_list", "{}"), execution);
        await invalid.Should().ThrowAsync<JsonException>().WithMessage("*missing a required property*");
        handler.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public async Task Direct_execution_requires_an_explicit_direct_allowlist_policy()
    {
        ClinicWorkspaceContext context = new(
            "practice-1", Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner", "en-US", "UTC", "conversation-1",
            SubjectKind: "practice");
        AgentExecutionContext execution = Execution(context, "run-1");
        TestTool handler = new("operations_activate_shift");
        TestCapability capability = new(
            "Operations",
            [],
            [handler],
            ActionPoliciesOverride:
            [ClinicWorkspaceActionPolicies.Direct(
                "activate_shift", "operations", [], ["TenantOwner"], [handler.Definition.Name])]);
        ClinicWorkspaceCapabilityRegistry registry = new(
            [capability],
            new ClinicWorkspaceAuthorizationPolicy(),
            new ClinicWorkspaceToolAvailabilityPolicy(),
            []);
        IAgentTool guarded = new ClinicWorkspaceToolDispatcher(registry, [])
            .CreateGuardedTools(
                context,
                execution,
                [handler],
                new Dictionary<string, string> { [handler.Definition.Name] = capability.Name })
            .Single();

        AgentToolResult result = await guarded.ExecuteAsync(
            new AgentToolCall("call-1", handler.Definition.Name, "{}"), execution);

        result.IsError.Should().BeFalse();
        handler.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public void Prepared_action_without_a_committer_fails_registration()
    {
        TestPreparationTool handler = new(Guid.NewGuid());
        TestCapability capability = new("Appointments", [], [handler], ["create_appointment"]);

        FluentActions.Invoking(() => new ClinicWorkspaceCapabilityRegistry(
                [capability],
                new ClinicWorkspaceAuthorizationPolicy(),
                new ClinicWorkspaceToolAvailabilityPolicy(),
                []))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*has no committer*");
    }

    [Fact]
    public async Task Prepared_action_rejects_a_non_preparation_handler()
    {
        ClinicWorkspaceContext context = PatientContext();
        AgentExecutionContext execution = Execution(context, "run-1");
        TestPreparationTool registeredHandler = new(Guid.NewGuid());
        TestTool handler = new("prepare_appointment");
        TestCapability capability = new("Appointments", [], [registeredHandler], ["create_appointment"]);
        ClinicWorkspaceCapabilityRegistry registry = new(
            [capability],
            new ClinicWorkspaceAuthorizationPolicy(),
            new ClinicWorkspaceToolAvailabilityPolicy(),
            [new TestCommitter("create_appointment", "Appointments")]);
        IAgentTool guarded = new ClinicWorkspaceToolDispatcher(registry, [])
            .CreateGuardedTools(
                context,
                execution,
                [handler],
                new Dictionary<string, string> { [handler.Definition.Name] = capability.Name })
            .Single();

        Func<Task> execute = () => guarded.ExecuteAsync(
            new AgentToolCall("call-1", handler.Definition.Name, "{}"), execution);

        await execute.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be prepared*");
        handler.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public void Capability_registration_rejects_a_prepared_action_with_a_non_preparation_tool()
    {
        TestTool handler = new("prepare_appointment");
        TestCapability capability = new("Appointments", [], [handler], ["create_appointment"]);

        FluentActions.Invoking(() => new ClinicWorkspaceCapabilityRegistry(
                [capability],
                new ClinicWorkspaceAuthorizationPolicy(),
                new ClinicWorkspaceToolAvailabilityPolicy(),
                [new TestCommitter("create_appointment", "Appointments")]))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*must reference a preparation tool*");
    }

    [Fact]
    public async Task Prepared_actions_are_collected_and_saved_during_the_runtime_callback()
    {
        Guid proposalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        TestPreparationTool preparationTool = new(proposalId);
        TestCapability capability = new("Appointments", [], [preparationTool], ["create_appointment"]);
        RecordingActionStore actionStore = new();
        RecordingSender sender = new();
        ClinicAssistantRuntime runtime = Runtime(sender, actionStore, [capability]);

        ClinicWorkspaceResult result = await runtime.RunAsync(
            PatientContext(), new ClinicWorkspaceInput("prepare an appointment"), CancellationToken.None);

        result.Actions.Should().ContainSingle().Which.ActionId.Should().Be(proposalId);
        actionStore.Saved.Should().ContainSingle().Which.ActionId.Should().Be(proposalId);
        sender.CallbackCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Model_output_cannot_create_an_action_without_a_server_preparation()
    {
        RecordingActionStore actionStore = new();
        RecordingSender sender = new(includeToolExecution: false);
        ClinicAssistantRuntime runtime = Runtime(
            sender,
            actionStore,
            [new TestCapability("Appointments", [], [new TestPreparationTool(Guid.NewGuid())], ["create_appointment"])]);

        ClinicWorkspaceResult result = await runtime.RunAsync(
            PatientContext(), new ClinicWorkspaceInput("create it"), CancellationToken.None);

        result.Actions.Should().BeEmpty();
        actionStore.Saved.Should().BeEmpty();
    }

    private static AgentProfileComposer<ClinicAssistantCompositionContext> Composer(
        IReadOnlyCollection<IClinicWorkspaceCapability> capabilities) =>
        new(new AgentCapabilityCatalog<ClinicAssistantCompositionContext>(
            ClinicAssistantCapabilityKeys.Patient
                .Concat(capabilities.Select(capability => capability.Name))
                .Distinct(StringComparer.Ordinal)
                .Select(key => (IAgentCapabilityPack<ClinicAssistantCompositionContext>)new ClinicAssistantCapabilityPack(key))));

    private static ClinicAssistantRuntime Runtime(
        RecordingSender sender,
        RecordingActionStore actionStore,
        IReadOnlyCollection<IClinicWorkspaceCapability> capabilities) =>
        CreateRuntime(
            Composer(capabilities),
            new ClinicWorkspaceCapabilityRegistry(
                capabilities,
                new ClinicWorkspaceAuthorizationPolicy(),
                new ClinicWorkspaceToolAvailabilityPolicy(),
                capabilities
                    .SelectMany(capability => capability.ActionPolicies)
                    .Select(policy => new TestCommitter(policy.ActionType, policy.OwningWorkflow))
                    .ToArray()),
            sender,
            actionStore);

    private static ClinicAssistantRuntime CreateRuntime(
        AgentProfileComposer<ClinicAssistantCompositionContext> composer,
        ClinicWorkspaceCapabilityRegistry registry,
        ISender sender,
        IClinicWorkspaceActionStore actionStore)
    {
        FixedTimeProvider time = new(DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        ClinicAssistantManifestComposer manifestComposer = new(
            composer,
            registry,
            new ClinicWorkspaceToolDispatcher(registry, []),
            new ClinicAssistantOptions("test-model"),
            [],
            new ClinicAssistantExecutionContextFactory(time));
        return new(
            new ClinicAssistantConversationRunner(
                manifestComposer,
                new ClinicAssistantResultParser(registry),
                sender,
                actionStore,
                registry,
                time),
            new ClinicAssistantToolResolver(manifestComposer));
    }

    private static ClinicWorkspaceContext PatientContext(IReadOnlySet<string>? permissions = null, string role = "Clinician") => new(
        "practice-1", PatientId, "user-1", permissions ?? new HashSet<string>(),
        role, "en-US", "UTC", "conversation-1");

    private static ClinicWorkspaceCapabilityRegistry TestRegistry() => new(
        [],
        new ClinicWorkspaceAuthorizationPolicy(),
        new ClinicWorkspaceToolAvailabilityPolicy(),
        []);

    private static AgentExecutionContext Execution(ClinicWorkspaceContext context, string runId) =>
        new(runId, new Dictionary<string, string>
        {
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["subjectKind"] = context.SubjectKind
        });

    private sealed record TestCapability(
        string Name,
        IReadOnlyCollection<string> Permissions,
        IReadOnlyCollection<IAgentTool> Tools,
        IReadOnlyCollection<string>? Actions = null,
        IReadOnlyCollection<ClinicWorkspaceActionPolicy>? ActionPoliciesOverride = null) : IClinicWorkspaceCapability
    {
        public string Description => $"{Name} test capability";
        public string GetInstructions() => $"Use {Name}.";
        public IReadOnlySet<string> RequiredPermissions { get; } = Permissions.ToHashSet(StringComparer.Ordinal);
        public IReadOnlyCollection<ClinicWorkspaceActionPolicy> ActionPolicies { get; } = ActionPoliciesOverride ??
            (Actions ?? []).Select(action => ClinicWorkspaceActionPolicies.Prepare(
                action,
                Name,
                Permissions,
                toolNames: Tools.Select(tool => tool.Definition.Name))).ToArray();
        public IReadOnlyCollection<string> ActionTypes => ActionPolicies.Select(policy => policy.ActionType).ToArray();
        public bool IsAvailable(ClinicWorkspaceContext context) => true;
    }

    private class TestTool(
        string name,
        string schema = "{\"type\":\"object\",\"additionalProperties\":false}") : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new(name, name, schema);
        public int ExecutionCount { get; private set; }

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new AgentToolResult(call.Id, "{}"));
        }
    }

    private sealed class TestCommitter(string actionType, string workflowKey) : IClinicWorkspaceActionCommitter
    {
        public string ActionType => actionType;
        public string WorkflowKey => workflowKey;

        public Task<ClinicWorkspaceActionCommitResult> CommitAsync(
            ClinicWorkspaceAction action,
            ClinicWorkspaceContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ClinicWorkspaceActionCommitResult.Completed("{}"));
    }

    private sealed class TestPreparationTool(Guid proposalId) : TestTool("prepare_appointment"), IClinicWorkspacePreparationTool
    {
        public ClinicWorkspacePreparedAction? GetPreparedAction(AgentToolResult result) =>
            result.IsError ? null : new(
                "Appointments", "create_appointment", proposalId, "{\"proposal\":true}",
                DateTimeOffset.Parse("2030-01-01T11:00:00Z"));
    }

    private sealed class RecordingSender(bool includeToolExecution = true) : ISender
    {
        public bool CallbackCompleted { get; private set; }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            RunAgentConversation conversation = request as RunAgentConversation
                ?? throw new InvalidOperationException("Unexpected request.");
            JsonElement output = JsonDocument.Parse(
                "{\"messages\":[{\"kind\":\"assistantMessage\",\"content\":\"Prepared\"}],\"actions\":[{\"capability\":\"Appointments\",\"actionType\":\"create_appointment\",\"summary\":\"Appointment\"}]}"
            ).RootElement.Clone();
            AgentRunResult run = new(true, null, output, new(0, 0), TimeSpan.Zero, 1, includeToolExecution ? 1 : 0)
            {
                History = "history",
                ToolExecutions = includeToolExecution
                    ? [new("prepare_appointment", "call-1", "prepared")]
                    : []
            };
            Guid conversationId = Guid.Parse("018f47c2-a6b7-7000-8000-000000000001");
            AgentConversationOutcome outcome = conversation.BeforeCommit is null
                ? AgentConversationOutcome.Succeeded
                : await conversation.BeforeCommit(conversationId, run, cancellationToken);
            CallbackCompleted = true;
            return (TResponse)(object)new RunAgentConversationResult(outcome, conversationId, run);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task ISender.Send<TRequest>(TRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingActionStore : IClinicWorkspaceActionStore
    {
        public List<ClinicWorkspaceAction> Saved { get; } = [];

        public Task SaveAsync(ClinicWorkspaceAction action, CancellationToken cancellationToken)
        {
            Saved.Add(action);
            return Task.CompletedTask;
        }

        public Task<ClinicWorkspaceAction?> GetAsync(Guid actionId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.SingleOrDefault(action => action.ActionId == actionId));
        public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> FailAsync(Guid actionId, ClinicWorkspaceActionFailure failure, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
