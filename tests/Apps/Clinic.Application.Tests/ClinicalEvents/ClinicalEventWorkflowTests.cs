using Xunit;
using FluentAssertions;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Application.PatientAgent.Capabilities.ClinicalEvents;

namespace Vitarish.Clinic.Application.Tests.ClinicalEvents;

public sealed class ClinicalEventWorkflowTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Prepare_persists_valid_clinician_authored_proposal_with_provenance()
    {
        Fixture fixture = new();
        ClinicalEventProposalResult result = await fixture.Prepare.ExecuteAsync(new(Scope(), Content(diagnosis: "Migraine", diagnosisAsserted: true)));

        result.Status.Should().Be(ClinicalEventProposalResultStatus.Prepared);
        result.Proposal.Should().BeEquivalentTo(fixture.Store.Proposal);
        result.Proposal!.ActionType.Should().Be("add_clinical_event");
        result.Proposal.Provenance.Should().Be(new ClinicalEventProvenance("run-1", "clinician-1", "AgentAssistedClinicianAssertion", Now));
        result.Proposal.Scope.PatientId.Should().Be(PatientId);
    }

    [Fact]
    public async Task Prepare_rejects_unknown_procedure_invalid_visit_missing_information_and_generated_diagnosis()
    {
        Fixture unknown = new() { Boundary = { Context = null } };
        (await unknown.Prepare.ExecuteAsync(new(Scope(), Content()))).Status.Should().Be(ClinicalEventProposalResultStatus.UnknownProcedure);

        Fixture invalidVisit = new();
        (await invalidVisit.Prepare.ExecuteAsync(new(Scope(), Content(visit: 3)))).Status.Should().Be(ClinicalEventProposalResultStatus.InvalidVisitNumber);

        Fixture missing = new();
        (await missing.Prepare.ExecuteAsync(new(Scope(), Content(note: null)))).Status.Should().Be(ClinicalEventProposalResultStatus.MissingRequiredInformation);

        Fixture diagnosis = new();
        (await diagnosis.Prepare.ExecuteAsync(new(Scope(), Content(diagnosis: "Generated", diagnosisAsserted: false)))).Status
            .Should().Be(ClinicalEventProposalResultStatus.GeneratedDiagnosisRejected);
    }

    [Fact]
    public async Task Prepare_denies_missing_create_permission_without_loading_procedure()
    {
        Fixture fixture = new();
        ClinicalEventScope denied = Scope(permissions: new HashSet<string> { "ClinicalEvents.Read" });
        (await fixture.Prepare.ExecuteAsync(new(denied, Content()))).Status.Should().Be(ClinicalEventProposalResultStatus.PermissionDenied);
        fixture.Boundary.ContextLoads.Should().Be(0);
    }

    [Fact]
    public async Task Confirm_rejects_scope_changes_stale_state_expiration_and_duplicate_confirmation()
    {
        Fixture scopeMismatch = await PreparedFixture();
        ClinicalEventScope otherPatient = Scope() with { PatientId = Guid.NewGuid() };
        (await scopeMismatch.Confirm.ExecuteAsync(scopeMismatch.Store.Proposal!.ActionId, otherPatient)).Status.Should().Be(ClinicalEventCommitResultStatus.ScopeMismatch);

        Fixture stale = await PreparedFixture();
        stale.Boundary.Context = stale.Boundary.Context! with { StateFingerprint = "changed" };
        (await stale.Confirm.ExecuteAsync(stale.Store.Proposal!.ActionId, Scope())).Status.Should().Be(ClinicalEventCommitResultStatus.StaleState);

        Fixture expired = await PreparedFixture();
        expired.Clock.UtcNow = Now.AddHours(1);
        (await expired.Confirm.ExecuteAsync(expired.Store.Proposal!.ActionId, Scope())).Status.Should().Be(ClinicalEventCommitResultStatus.Expired);

        Fixture duplicate = await PreparedFixture();
        ClinicalEventCommitResult first = await duplicate.Confirm.ExecuteAsync(duplicate.Store.Proposal!.ActionId, Scope());
        ClinicalEventCommitResult second = await duplicate.Confirm.ExecuteAsync(duplicate.Store.Proposal.ActionId, Scope());
        first.Status.Should().Be(ClinicalEventCommitResultStatus.Completed);
        second.Status.Should().Be(ClinicalEventCommitResultStatus.AlreadyConfirmed);
        duplicate.Boundary.AddCalls.Should().Be(1);
    }

    [Fact]
    public async Task Confirmation_sends_only_generic_title_and_opaque_payload_to_boundary()
    {
        Fixture fixture = await PreparedFixture();
        await fixture.Confirm.ExecuteAsync(fixture.Store.Proposal!.ActionId, Scope());

        fixture.Boundary.Title.Should().Be("Visit 2: Follow-up");
        fixture.Boundary.Payload.Should().Contain("NarrativeNote").And.Contain("Clinician note");
    }

    [Fact]
    public void Model_run_exposes_only_context_and_preparation_tools_without_scope_identifiers()
    {
        Fixture fixture = new();
        var contextTool = new GetPatientProcedureContextTool(new GetPatientProcedureContext(fixture.Boundary));
        var prepareTool = new PrepareClinicalEventTool(fixture.Prepare);
        var capability = new ClinicalEventsPatientAgentCapability(contextTool, prepareTool);

        capability.Tools.Select(tool => tool.Definition.Name).Should().Equal(
            "get_patient_procedure_context", "prepare_clinical_event");
        capability.Tools.Select(tool => tool.Definition.ParametersJsonSchema).Should().OnlyContain(schema =>
            !schema.Contains("patientId", StringComparison.OrdinalIgnoreCase) &&
            !schema.Contains("practiceId", StringComparison.OrdinalIgnoreCase) &&
            !schema.Contains("clinicianId", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<Fixture> PreparedFixture()
    {
        Fixture fixture = new();
        await fixture.Prepare.ExecuteAsync(new(Scope(), Content()));
        return fixture;
    }

    private static ClinicalEventScope Scope(IReadOnlySet<string>? permissions = null) => new(
        PatientId, "practice-1", "clinician-1",
        permissions ?? new HashSet<string> { "ClinicalEvents.Read", "ClinicalEvents.Create" },
        "run-1", "en", "UTC");

    private static ClinicalEventContent Content(int visit = 2, string? note = "Clinician note", string? diagnosis = null, bool diagnosisAsserted = false) =>
        new(ProcedureId, visit, "Follow-up", "Completed", Now, null, diagnosis, diagnosisAsserted, null, [], note);

    private sealed class Fixture
    {
        public Boundary Boundary { get; } = new();
        public Store Store { get; } = new();
        public Clock Clock { get; } = new() { UtcNow = Now };
        public PrepareClinicalEvent Prepare => new(Boundary, Store, Clock);
        public ConfirmClinicalEventProposal Confirm => new(Boundary, Store, Clock);
    }

    private sealed class Boundary : IClinicalEventProcedureBoundary
    {
        public PatientProcedureContext? Context { get; set; } = new(PatientId, ProcedureId, "Procedure", 2, Guid.NewGuid(), "state");
        public int ContextLoads { get; private set; }
        public int AddCalls { get; private set; }
        public string? Title { get; private set; }
        public string? Payload { get; private set; }
        public Task<PatientProcedureContext?> GetContextAsync(Guid patientId, string practiceId, Guid procedureId, CancellationToken cancellationToken)
        { ContextLoads++; return Task.FromResult(Context); }
        public Task<Guid?> AddEventAsync(PatientProcedureContext context, string title, string opaquePayload, CancellationToken cancellationToken)
        { AddCalls++; Title = title; Payload = opaquePayload; return Task.FromResult<Guid?>(Guid.NewGuid()); }
    }

    private sealed class Store : IClinicalEventProposalStore
    {
        public ClinicalEventProposal? Proposal { get; private set; }
        public Task SaveAsync(ClinicalEventProposal proposal, CancellationToken cancellationToken) { Proposal = proposal; return Task.CompletedTask; }
        public Task<ClinicalEventProposal?> GetAsync(Guid actionId, CancellationToken cancellationToken) => Task.FromResult(Proposal?.ActionId == actionId ? Proposal : null);
        public Task<bool> CompleteAsync(Guid actionId, Guid eventId, CancellationToken cancellationToken)
        { if (Proposal is not { Status: ClinicalEventProposalStatus.Prepared } value) return Task.FromResult(false); Proposal = value with { Status = ClinicalEventProposalStatus.Completed, CreatedEventId = eventId }; return Task.FromResult(true); }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
