using FluentAssertions;
using System.Text.Json;
using Vitarish.Clinic.Agents;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.PatientAgent;

public sealed class ClinicAgentCompositionTests
{
    [Fact]
    public void Patient_and_operations_profiles_expose_distinct_tools_and_identities()
    {
        TestTool patientTool = new("patient_tool");
        TestTool operationsTool = new("operations_tool");
        ClinicWorkspaceAgentComposer composer = Composer(
            [
                new TestCapability("Appointments", [patientTool], patient: true),
                new TestCapability("Operations", [operationsTool], practice: true)
            ]);

        PatientWorkspaceAgentDefinition patient = composer.Compose(Context());
        PatientWorkspaceAgentDefinition operations = composer.Compose(Context(subjectKind: "practice", patientId: Guid.Empty));

        patient.Definition.Name.Should().Be("Clinic patient workspace");
        patient.Tools.Select(tool => tool.Definition.Name).Should().Equal("patient_tool");
        operations.Definition.Name.Should().Be("Clinic operations workspace");
        operations.Tools.Select(tool => tool.Definition.Name).Should().Equal("operations_tool");
        patient.CompatibilityKey.Should().NotBe(operations.CompatibilityKey);
    }

    [Fact]
    public void Capability_denial_removes_the_model_surface_and_changes_the_key()
    {
        TestTool tool = new("patient_tool");
        ClinicWorkspaceAgentComposer allowed = Composer([new TestCapability("Appointments", [tool], requiredPermission: "Appointments.Read")]);
        ClinicWorkspaceAgentComposer denied = Composer([new TestCapability("Appointments", [tool], requiredPermission: "Appointments.Read")]);

        PatientWorkspaceAgentDefinition exposed = allowed.Compose(Context(permissions: new HashSet<string> { "Appointments.Read" }));
        PatientWorkspaceAgentDefinition hidden = denied.Compose(Context());

        exposed.Tools.Should().ContainSingle().Which.Definition.Name.Should().Be("patient_tool");
        hidden.Tools.Should().BeEmpty();
        exposed.CompatibilityKey.Should().NotBe(hidden.CompatibilityKey);
    }

    [Fact]
    public void Available_capabilities_outside_the_active_profile_do_not_reach_the_model()
    {
        TestTool selectedTool = new("patient_tool");
        TestTool unselectedTool = new("management_tool");
        ClinicWorkspaceAgentComposer composer = Composer(
        [
            new TestCapability("Appointments", [selectedTool]),
            new TestCapability("SeaManagement", [unselectedTool])
        ]);

        PatientWorkspaceAgentDefinition definition = composer.Compose(Context());

        definition.Tools.Select(tool => tool.Definition.Name).Should().Equal("patient_tool");
        definition.Definition.Instructions.Should().Contain("Appointments test capability")
            .And.NotContain("SeaManagement test capability")
            .And.NotContain("Use SeaManagement.");
    }

    [Fact]
    public void Compatibility_key_is_stable_across_time_but_changes_for_semantic_context_or_schema_changes()
    {
        TestTool firstTool = new("patient_tool");
        MutableTimeProvider clock = new(DateTimeOffset.Parse("2026-08-12T10:00:00Z"));
        ClinicWorkspaceAgentComposer composer = Composer([new TestCapability("Appointments", [firstTool])], clock);

        PatientAgentContext stableContext = Context();
        PatientWorkspaceAgentDefinition first = composer.Compose(stableContext);
        clock.UtcNow = clock.UtcNow.AddHours(3);
        PatientWorkspaceAgentDefinition later = composer.Compose(stableContext);
        PatientWorkspaceAgentDefinition differentLocale = composer.Compose(Context(locale: "fa-IR"));
        ClinicWorkspaceAgentComposer changedSchema = Composer([new TestCapability("Appointments", [new TestTool("patient_tool", "{\"type\":\"string\"}")])], clock);

        first.CompatibilityKey.Should().Be(later.CompatibilityKey);
        first.CompatibilityKey.Should().NotBe(differentLocale.CompatibilityKey);
        first.CompatibilityKey.Should().NotBe(changedSchema.Compose(Context()).CompatibilityKey);
        first.Definition.Instructions.Should().NotContain("<current-utc>");
    }

    [Fact]
    public void Registry_rejects_duplicate_tools_and_invalid_schemas_before_composition()
    {
        TestTool duplicate = new("duplicate");
        Action duplicateAction = () => _ = new PatientAgentCapabilityRegistry(
            [new TestCapability("Appointments", [duplicate]), new TestCapability("Prescriptions", [duplicate])],
            new PatientAgentAuthorizationPolicy(), new PatientAgentToolAvailabilityPolicy());
        Action invalidSchema = () => _ = new PatientAgentCapabilityRegistry(
            [new TestCapability("Appointments", [new TestTool("invalid", "not-json")])],
            new PatientAgentAuthorizationPolicy(), new PatientAgentToolAvailabilityPolicy());

        duplicateAction.Should().Throw<InvalidOperationException>().WithMessage("*stable and unique*");
        invalidSchema.Should().Throw<JsonException>();
    }

    private static ClinicWorkspaceAgentComposer Composer(
        IEnumerable<IPatientAgentCapability> capabilities,
        TimeProvider? clock = null)
    {
        List<IAgentCapabilityPack<ClinicAgentCompositionContext>> packs =
        [
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Appointments),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.AppointmentReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Prescriptions),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.PrescriptionReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.PatientReminderReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.ClinicPatientReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.SeaPatientReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.ClinicalEvents),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.SeaManagement),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Operations),
            new ClinicIntelligenceCapabilityPack()
        ];
        return new(
            new AgentProfileComposer<ClinicAgentCompositionContext>(new AgentCapabilityCatalog<ClinicAgentCompositionContext>(packs)),
            new PatientAgentCapabilityRegistry(capabilities, new PatientAgentAuthorizationPolicy(), new PatientAgentToolAvailabilityPolicy()),
            new PatientAgentToolDispatcher(new PatientAgentApprovalPolicy(), []),
            new PatientWorkspaceAgentOptions("test-model"),
            [],
            clock ?? TimeProvider.System);
    }

    private static PatientAgentContext Context(
        string subjectKind = "patient",
        Guid? patientId = null,
        IReadOnlySet<string>? permissions = null,
        string locale = "en-US") =>
        new("practice-1", patientId ?? (subjectKind == "practice" ? Guid.Empty : Guid.NewGuid()), "user-1",
            permissions ?? new HashSet<string>(), "TenantOwner", locale, "UTC", "conversation-1", SubjectKind: subjectKind);

    private sealed class TestCapability(
        string name,
        IReadOnlyCollection<IAgentTool> tools,
        bool patient = false,
        bool practice = false,
        string? requiredPermission = null) : IPatientAgentCapability
    {
        public string Name => name;
        public string Description => $"{name} test capability";
        public string GetInstructions() => $"Use {name}.";
        public IReadOnlySet<string> RequiredPermissions { get; } = requiredPermission is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>([requiredPermission], StringComparer.Ordinal);
        public IReadOnlyCollection<IAgentTool> Tools => tools;
        public IReadOnlyCollection<string> ActionTypes => [];
        public bool IsAvailable(PatientAgentContext context) =>
            (patient && context.SubjectKind == "patient") || (practice && context.SubjectKind == "practice") || (!patient && !practice);
    }

    private sealed class TestTool(string name, string schema = "{\"type\":\"object\",\"additionalProperties\":false}") : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new(name, name, schema);
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
