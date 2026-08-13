using FluentAssertions;
using Vitarish.Clinic.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests;

public sealed class ClinicAssistantCompositionTests
{
    [Fact]
    public void Patient_and_operations_profiles_use_the_new_durable_keys()
    {
        ComposedAgentManifest<ClinicAssistantCompositionContext> patient = Compose(Context(), [new TestCapability("Appointments", [new TestTool("patient_tool")])]);
        ComposedAgentManifest<ClinicAssistantCompositionContext> operations = Compose(
            Context(subjectKind: "practice", patientId: Guid.Empty),
            [new TestCapability("Operations", [new TestTool("operations_tool")])]);

        patient.Definition.Name.Should().Be("Clinic patient workspace");
        patient.Definition.Instructions.Should().Contain("opaque server-bound reference")
            .And.NotContain("11111111-1111-1111-1111-111111111111");
        patient.Tools.Select(tool => tool.Definition.Name).Should().Equal("patient_tool");
        operations.Definition.Name.Should().Be("Clinic operations workspace");
        operations.Tools.Select(tool => tool.Definition.Name).Should().Equal("operations_tool");
        patient.CompatibilityKey.Should().NotBe(operations.CompatibilityKey);
        patient.Definition.Instructions.Should().NotContain("<current-utc>");
    }

    [Fact]
    public void Denied_capabilities_are_not_exposed_and_change_the_compatibility_key()
    {
        TestCapability capability = new("Appointments", [new TestTool("appointments")]);
        ComposedAgentManifest<ClinicAssistantCompositionContext> allowed = Compose(Context(), [capability], allow: true);
        ComposedAgentManifest<ClinicAssistantCompositionContext> denied = Compose(Context(), [capability], allow: false);

        allowed.Tools.Should().ContainSingle();
        denied.Tools.Should().BeEmpty();
        allowed.CompatibilityKey.Should().NotBe(denied.CompatibilityKey);
    }

    [Fact]
    public void Dynamic_time_is_excluded_from_compatibility_material()
    {
        MutableTimeProvider clock = new(DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        ClinicAssistantCompositionContext context = new(Context(), [], [], clock.GetUtcNow());
        AgentProfileComposer<ClinicAssistantCompositionContext> composer = new(new AgentCapabilityCatalog<ClinicAssistantCompositionContext>(Packs([])));
        ComposedAgentManifest<ClinicAssistantCompositionContext> first = composer.Compose(new ClinicPatientAssistantProfile(context, new("test-model")), context, _ => new(true, "enabled"));
        clock.UtcNow = clock.UtcNow.AddHours(4);
        ComposedAgentManifest<ClinicAssistantCompositionContext> later = composer.Compose(
            new ClinicPatientAssistantProfile(context with { CurrentUtc = clock.GetUtcNow() }, new("test-model")),
            context with { CurrentUtc = clock.GetUtcNow() },
            _ => new(true, "enabled"));

        first.CompatibilityKey.Should().Be(later.CompatibilityKey);
    }

    private static ComposedAgentManifest<ClinicAssistantCompositionContext> Compose(
        AssistantContext context,
        IReadOnlyCollection<IAssistantCapability> capabilities,
        bool allow = true)
    {
        ClinicAssistantCompositionContext composition = new(context, capabilities, [], DateTimeOffset.Parse("2030-01-01T10:00:00Z"));
        AgentProfileComposer<ClinicAssistantCompositionContext> composer = new(new AgentCapabilityCatalog<ClinicAssistantCompositionContext>(Packs(capabilities)));
        AgentProfile<ClinicAssistantCompositionContext> profile = context.SubjectKind == "practice"
            ? new ClinicOperationsAssistantProfile(composition, new("test-model"))
            : new ClinicPatientAssistantProfile(composition, new("test-model"));
        return composer.Compose(profile, composition, _ => allow ? new(true, "enabled") : AgentExposureDecision.Denied("denied"));
    }

    private static IEnumerable<IAgentCapabilityPack<ClinicAssistantCompositionContext>> Packs(
        IReadOnlyCollection<IAssistantCapability> capabilities) =>
        ClinicAssistantCapabilityKeys.Patient
            .Concat(ClinicAssistantCapabilityKeys.OperationsProfile)
            .Distinct(StringComparer.Ordinal)
            .Select(key => (IAgentCapabilityPack<ClinicAssistantCompositionContext>)new ClinicAssistantCapabilityPack(key));

    private static AssistantContext Context(string subjectKind = "patient", Guid? patientId = null) => new(
        "practice-1",
        patientId ?? (subjectKind == "practice" ? Guid.Empty : Guid.Parse("11111111-1111-1111-1111-111111111111")),
        "user-1",
        new HashSet<string>(),
        "TenantOwner",
        "en-US",
        "UTC",
        "conversation-1",
        SubjectKind: subjectKind);

    private sealed record TestCapability(string Name, IReadOnlyCollection<IAgentTool> Tools) : IAssistantCapability
    {
        public string Description => $"{Name} test capability";
        public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>();
        public IReadOnlyCollection<AssistantActionPolicy> ActionPolicies { get; } = [];
        public IReadOnlyCollection<string> ActionTypes => [];
        public string GetInstructions() => $"Use {Name}.";
        public bool IsAvailable(AssistantContext context) => true;
    }

    private sealed class TestTool(string name) : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new(name, name, "{\"type\":\"object\"}");
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
