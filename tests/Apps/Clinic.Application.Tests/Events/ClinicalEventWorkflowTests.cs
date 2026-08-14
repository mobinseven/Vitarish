using FluentAssertions;
using Vitarish.Clinic.Application.Events;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Events;

public sealed class ClinicalEventWorkflowTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Prepare_persists_valid_clinician_authored_proposal_with_provenance()
    {
        Fixture fixture = new();
        ClinicalEventProposalResult result = await fixture.Prepare.ExecuteAsync(
            new(Scope(), Content(diagnosis: "Migraine", diagnosisAsserted: true)));

        result.Status.Should().Be(ClinicalEventProposalResultStatus.Prepared);
        result.Proposal.Should().BeEquivalentTo(fixture.Store.Proposal);
        result.Proposal!.ActionType.Should().Be("add_clinical_event");
        result.Proposal.Provenance.Should().Be(
            new ClinicalEventProvenance(
                "run-1",
                "clinician-1",
                "AgentAssistedClinicianAssertion",
                Now));
        result.Proposal.ExpiresAt.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public async Task Prepare_rejects_invalid_inputs_and_generated_diagnosis()
    {
        Fixture unknown = new();
        unknown.Boundary.Context = null;
        (await unknown.Prepare.ExecuteAsync(new(Scope(), Content()))).Status
            .Should().Be(ClinicalEventProposalResultStatus.UnknownProcedure);

        Fixture invalidVisit = new();
        (await invalidVisit.Prepare.ExecuteAsync(new(Scope(), Content(visit: 3)))).Status
            .Should().Be(ClinicalEventProposalResultStatus.InvalidVisitNumber);

        Fixture missing = new();
        (await missing.Prepare.ExecuteAsync(new(Scope(), Content(note: null)))).Status
            .Should().Be(ClinicalEventProposalResultStatus.MissingRequiredInformation);

        Fixture diagnosis = new();
        (await diagnosis.Prepare.ExecuteAsync(
            new(Scope(), Content(diagnosis: "Generated", diagnosisAsserted: false)))).Status
            .Should().Be(ClinicalEventProposalResultStatus.GeneratedDiagnosisRejected);
    }

    [Fact]
    public async Task Prepare_denies_missing_create_permission_before_loading_procedure()
    {
        Fixture fixture = new();
        ClinicalEventScope denied = Scope(
            new HashSet<string> { "ClinicalEvents.Read" });

        (await fixture.Prepare.ExecuteAsync(new(denied, Content()))).Status
            .Should().Be(ClinicalEventProposalResultStatus.PermissionDenied);
        fixture.Boundary.ContextLoads.Should().Be(0);
    }

    [Fact]
    public async Task Prepare_rejects_oversized_untrusted_content_before_loading_procedure()
    {
        Fixture fixture = new();
        ClinicalEventContent oversized = Content(note: new string('x', 70 * 1024));

        (await fixture.Prepare.ExecuteAsync(new(Scope(), oversized))).Status
            .Should().Be(ClinicalEventProposalResultStatus.MissingRequiredInformation);
        fixture.Boundary.ContextLoads.Should().Be(0);
    }

    [Fact]
    public async Task Confirm_rejects_scope_changes_stale_state_and_expiration()
    {
        Fixture scopeMismatch = await PreparedFixture();
        ClinicalEventScope otherPatient = Scope() with { PatientId = Guid.NewGuid() };
        (await scopeMismatch.Confirm.ExecuteAsync(
            scopeMismatch.Store.Proposal!.ActionId,
            otherPatient)).Status.Should().Be(ClinicalEventCommitResultStatus.ScopeMismatch);

        Fixture stale = await PreparedFixture();
        stale.Boundary.Context = stale.Boundary.Context! with
        {
            StateFingerprint = "changed"
        };
        (await stale.Confirm.ExecuteAsync(stale.Store.Proposal!.ActionId, Scope())).Status
            .Should().Be(ClinicalEventCommitResultStatus.StaleState);

        Fixture expired = await PreparedFixture();
        expired.Clock.UtcNow = Now.AddHours(1);
        (await expired.Confirm.ExecuteAsync(expired.Store.Proposal!.ActionId, Scope())).Status
            .Should().Be(ClinicalEventCommitResultStatus.Expired);
    }

    [Fact]
    public async Task Confirm_claims_before_mutation_and_duplicate_does_not_add_again()
    {
        Fixture fixture = await PreparedFixture();

        ClinicalEventCommitResult first = await fixture.Confirm.ExecuteAsync(
            fixture.Store.Proposal!.ActionId,
            Scope());
        ClinicalEventCommitResult second = await fixture.Confirm.ExecuteAsync(
            fixture.Store.Proposal.ActionId,
            Scope());

        first.Status.Should().Be(ClinicalEventCommitResultStatus.Completed);
        second.Status.Should().Be(ClinicalEventCommitResultStatus.AlreadyConfirmed);
        second.EventId.Should().Be(first.EventId);
        fixture.Boundary.AddCalls.Should().Be(1);
        fixture.Store.BeganConfirmationBeforeAdd.Should().BeTrue();
    }

    [Fact]
    public async Task Confirm_keeps_claim_after_ambiguous_mutation_failure()
    {
        Fixture fixture = await PreparedFixture();
        fixture.Boundary.AddException = new TimeoutException("Ambiguous SEA result.");

        Func<Task> act = () => fixture.Confirm.ExecuteAsync(
            fixture.Store.Proposal!.ActionId,
            Scope());

        await act.Should().ThrowAsync<TimeoutException>();
        fixture.Store.Proposal!.Status.Should().Be(ClinicalEventProposalStatus.Confirming);
        (await fixture.Confirm.ExecuteAsync(fixture.Store.Proposal.ActionId, Scope())).Status
            .Should().Be(ClinicalEventCommitResultStatus.ConfirmationInProgress);
        fixture.Boundary.AddCalls.Should().Be(1);
    }

    [Fact]
    public async Task Confirm_releases_claim_when_boundary_reports_no_mutation()
    {
        Fixture fixture = await PreparedFixture();
        fixture.Boundary.EventId = null;

        ClinicalEventCommitResult result = await fixture.Confirm.ExecuteAsync(
            fixture.Store.Proposal!.ActionId,
            Scope());

        result.Status.Should().Be(ClinicalEventCommitResultStatus.ProcedureUnavailable);
        fixture.Store.Proposal!.Status.Should().Be(ClinicalEventProposalStatus.Prepared);
    }

    [Fact]
    public async Task Confirmation_sends_only_generic_title_and_opaque_payload_to_boundary()
    {
        Fixture fixture = await PreparedFixture();
        await fixture.Confirm.ExecuteAsync(fixture.Store.Proposal!.ActionId, Scope());

        fixture.Boundary.Title.Should().Be("Visit 2: Follow-up");
        fixture.Boundary.Payload.Should().Contain("NarrativeNote")
            .And.Contain("Clinician note");
    }

    private static async Task<Fixture> PreparedFixture()
    {
        Fixture fixture = new();
        await fixture.Prepare.ExecuteAsync(new(Scope(), Content()));
        return fixture;
    }

    private static ClinicalEventScope Scope(
        IReadOnlySet<string>? permissions = null) =>
        new(
            PatientId,
            "practice-1",
            "clinician-1",
            permissions ??
            new HashSet<string>
            {
                "ClinicalEvents.Read",
                "ClinicalEvents.Create"
            },
            "run-1",
            "en",
            "UTC");

    private static ClinicalEventContent Content(
        int visit = 2,
        string? note = "Clinician note",
        string? diagnosis = null,
        bool diagnosisAsserted = false) =>
        new(
            ProcedureId,
            visit,
            "Follow-up",
            "Completed",
            Now,
            null,
            diagnosis,
            diagnosisAsserted,
            null,
            [],
            note);

    private sealed class Fixture
    {
        public Boundary Boundary { get; } = new();
        public Store Store { get; }
        public Clock Clock { get; } = new() { UtcNow = Now };
        public Fixture()
        {
            Store = new(Boundary);
        }
        public PrepareClinicalEvent Prepare => new(Boundary, Store, Clock);
        public ConfirmClinicalEventProposal Confirm => new(Boundary, Store, Clock);
    }

    private sealed class Boundary : IClinicalEventProcedureBoundary
    {
        public PatientProcedureContext? Context { get; set; } =
            new(PatientId, ProcedureId, "Procedure", 2, Guid.NewGuid(), "state");
        public Guid? EventId { get; set; } = Guid.NewGuid();
        public Exception? AddException { get; set; }
        public int ContextLoads { get; private set; }
        public int AddCalls { get; private set; }
        public string? Title { get; private set; }
        public string? Payload { get; private set; }
        public Store? Store { get; set; }

        public Task<PatientProcedureContext?> GetContextAsync(
            Guid patientId,
            string practiceId,
            Guid procedureId,
            CancellationToken cancellationToken)
        {
            ContextLoads++;
            return Task.FromResult(Context);
        }

        public Task<Guid?> AddEventAsync(
            PatientProcedureContext context,
            string title,
            string opaquePayload,
            CancellationToken cancellationToken)
        {
            AddCalls++;
            Title = title;
            Payload = opaquePayload;
            if (AddException is not null) throw AddException;
            return Task.FromResult(EventId);
        }
    }

    private sealed class Store(Boundary boundary) : IClinicalEventProposalStore
    {
        public ClinicalEventProposal? Proposal { get; private set; }
        public bool BeganConfirmationBeforeAdd { get; private set; }

        public Task SaveAsync(
            ClinicalEventProposal proposal,
            CancellationToken cancellationToken)
        {
            Proposal = proposal;
            return Task.CompletedTask;
        }

        public Task<ClinicalEventProposal?> GetAsync(
            Guid actionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Proposal?.ActionId == actionId ? Proposal : null);

        public Task<bool> TryBeginConfirmationAsync(
            Guid actionId,
            CancellationToken cancellationToken)
        {
            if (Proposal is not { Status: ClinicalEventProposalStatus.Prepared } value)
                return Task.FromResult(false);
            Proposal = value with { Status = ClinicalEventProposalStatus.Confirming };
            BeganConfirmationBeforeAdd = boundary.AddCalls == 0;
            return Task.FromResult(true);
        }

        public Task<bool> TryReleaseConfirmationAsync(
            Guid actionId,
            CancellationToken cancellationToken)
        {
            if (Proposal is not { Status: ClinicalEventProposalStatus.Confirming } value)
                return Task.FromResult(false);
            Proposal = value with { Status = ClinicalEventProposalStatus.Prepared };
            return Task.FromResult(true);
        }

        public Task<bool> CompleteAsync(
            Guid actionId,
            Guid eventId,
            CancellationToken cancellationToken)
        {
            if (Proposal is not { Status: ClinicalEventProposalStatus.Confirming } value)
                return Task.FromResult(false);
            Proposal = value with
            {
                Status = ClinicalEventProposalStatus.Completed,
                CreatedEventId = eventId
            };
            return Task.FromResult(true);
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
