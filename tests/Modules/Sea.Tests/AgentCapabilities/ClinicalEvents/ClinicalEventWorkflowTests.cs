using FluentAssertions;
using Vitarish.Sea.Application.AgentCapabilities.ClinicalEvents;
using Vitarish.Sea.Interface.AgentCapabilities;

namespace Vitarish.Sea.Tests.AgentCapabilities.ClinicalEvents;

public sealed class ClinicalEventWorkflowTests
{
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly ClinicalEventScope _scope;
    private readonly FakeBoundary _boundary;
    private readonly IClinicalEventProposalStore _store = new InMemoryClinicalEventProposalStore();

    public ClinicalEventWorkflowTests()
    {
        _scope = new(_patientId, "practice", "clinician");
        _boundary = new(new(_procedureId, _patientId, _patientId, "Procedure", new HashSet<int> { 2 }, "v1", true));
    }

    [Fact]
    public async Task Valid_visit_preserves_structured_clinician_diagnosis_and_commits_once()
    {
        ClinicalEventProposalResult prepared = await Prepare().ExecuteAsync(Request(diagnosis: "Clinician diagnosis"), default);

        prepared.Status.Should().Be(ClinicalEventProposalStatus.Prepared);
        prepared.Proposal!.StructuredContent.ClinicianAuthoredDiagnosisText.Should().Be("Clinician diagnosis");
        prepared.Proposal.StructuredContent.NarrativeNote.Should().Contain("Clinician-stated diagnosis: Clinician diagnosis");
        prepared.Proposal.Provenance.Should().BeEquivalentTo(new ClinicalEventProvenance("AgentAssisted", "run", "clinician", null, true));

        ConfirmClinicalEventProposal confirm = Confirm();
        ClinicalEventCommitResult committed = await confirm.ExecuteAsync(prepared.Proposal.Id, _scope, "clinician", default);
        ClinicalEventCommitResult duplicate = await confirm.ExecuteAsync(prepared.Proposal.Id, _scope, "clinician", default);

        committed.Status.Should().Be(ClinicalEventCommitStatus.Committed);
        committed.Provenance!.ConfirmedByUserId.Should().Be("clinician");
        duplicate.Status.Should().Be(ClinicalEventCommitStatus.AlreadyConfirmed);
        _boundary.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task Unknown_procedure_is_rejected() =>
        (await new PrepareClinicalEvent(new GetPatientProcedureContext(new FakeBoundary(null)), _store, TimeProvider.System)
            .ExecuteAsync(Request(), default)).Status.Should().Be(ClinicalEventProposalStatus.UnknownProcedure);

    [Fact]
    public async Task Invalid_visit_number_is_rejected() =>
        (await Prepare().ExecuteAsync(Request(visit: 3), default)).Status.Should().Be(ClinicalEventProposalStatus.InvalidVisitNumber);

    [Fact]
    public async Task Model_added_diagnosis_is_rejected() =>
        (await Prepare().ExecuteAsync(Request(generatedDiagnosis: "Unsupported diagnosis"), default)).Status.Should().Be(ClinicalEventProposalStatus.UnsupportedDiagnosis);

    [Fact]
    public async Task Missing_required_event_fields_are_reported()
    {
        PrepareClinicalEventRequest request = Request() with { EventType = null, CompletionStatus = null, PerformedAt = null, ClinicianAuthoredObservations = null, PlanOrFollowUp = null };
        ClinicalEventProposalResult result = await Prepare().ExecuteAsync(request, default);
        result.Status.Should().Be(ClinicalEventProposalStatus.MissingInformation);
        result.MissingFields.Should().BeEquivalentTo("eventType", "completionStatus", "performedAt", "clinicalContent");
    }

    [Fact]
    public async Task Unauthorized_user_cannot_prepare_or_confirm()
    {
        _boundary.Context = _boundary.Context! with { CanRecordClinicalEvent = false };
        (await Prepare().ExecuteAsync(Request(), default)).Status.Should().Be(ClinicalEventProposalStatus.Forbidden);
        _boundary.Context = _boundary.Context with { CanRecordClinicalEvent = true };
        ClinicalEventProposal proposal = (await Prepare().ExecuteAsync(Request(), default)).Proposal!;
        ClinicalEventScope other = _scope with { UserId = "other" };
        (await Confirm().ExecuteAsync(proposal.Id, other, "other", default)).Status.Should().Be(ClinicalEventCommitStatus.Forbidden);
    }

    [Fact]
    public async Task Stale_procedure_state_requires_a_new_proposal()
    {
        ClinicalEventProposal proposal = (await Prepare().ExecuteAsync(Request(), default)).Proposal!;
        _boundary.Context = _boundary.Context! with { StateVersion = "v2" };
        (await Confirm().ExecuteAsync(proposal.Id, _scope, "clinician", default)).Status.Should().Be(ClinicalEventCommitStatus.StaleProcedureState);
        _boundary.CommitCount.Should().Be(0);
    }

    private PrepareClinicalEvent Prepare() => new(new GetPatientProcedureContext(_boundary), _store, TimeProvider.System);
    private ConfirmClinicalEventProposal Confirm() => new(new GetPatientProcedureContext(_boundary), _store, _boundary);
    private PrepareClinicalEventRequest Request(int visit = 2, string? diagnosis = null, string? generatedDiagnosis = null) =>
        new(_scope, _procedureId, visit, "ProcedureVisit", "Completed", DateTimeOffset.Parse("2026-07-28T10:00:00Z"),
            "Healing appropriately", diagnosis, "Review in two weeks", ["attachment-1"], generatedDiagnosis,
            new("AgentAssisted", "run", "clinician", null));

    private sealed class FakeBoundary(PatientProcedureContext? context) : IClinicalEventProcedureBoundary
    {
        public PatientProcedureContext? Context { get; set; } = context;
        public int CommitCount { get; private set; }
        public Task<PatientProcedureContext?> GetContextAsync(ClinicalEventScope scope, Guid procedureId, CancellationToken cancellationToken) =>
            Task.FromResult(Context is not null && Context.ProcedureId == procedureId ? Context : null);
        public Task<Guid?> AddClinicalEventItemAsync(ClinicalEventScope scope, ClinicalEventContent content, ClinicalEventProvenance provenance, CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
    }
}
