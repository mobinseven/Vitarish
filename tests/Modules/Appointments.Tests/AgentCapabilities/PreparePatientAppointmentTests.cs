using FluentAssertions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Application.Commands;
using Vitarish.Entities.Contracts.Models;
using MediatR;
using Moq;

namespace Vitarish.Appointments.Tests.AgentCapabilities;

public sealed class PreparePatientAppointmentTests
{
    [Fact]
    public async Task Prepare_returns_exact_immutable_available_slot()
    {
        AppointmentSlot slot = Slot();
        Store store = new();
        PreparePatientAppointment sut = Create(new Availability(slot), store);

        AppointmentProposalResult result = await sut.ExecuteAsync(Request(), CancellationToken.None);

        result.Status.Should().Be(AppointmentProposalResultStatus.Prepared);
        result.Proposal!.Slot.Should().Be(slot);
        (await store.GetProposalAsync(result.Proposal.Id, result.Proposal.PracticeId, default)).Should().Be(result.Proposal);
    }

    [Theory]
    [InlineData("2026-10-25T01:30", 30, "Consultation", AppointmentProposalResultStatus.AmbiguousDate)]
    [InlineData("2026-08-12T09:00", null, "Consultation", AppointmentProposalResultStatus.MissingInformation)]
    [InlineData("2026-08-12T09:00", 30, null, AppointmentProposalResultStatus.MissingInformation)]
    public async Task Prepare_rejects_ambiguous_or_missing_material_information(string local, int? duration, string? service, AppointmentProposalResultStatus expected)
    {
        PreparePatientAppointment sut = Create(new Availability(Slot()), new Store());
        AppointmentProposalResult result = await sut.ExecuteAsync(Request(local, duration, service), CancellationToken.None);
        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task Prepare_reports_unavailable_without_saving_a_proposal()
    {
        AppointmentProposalResult result = await Create(new Availability(null), new Store())
            .ExecuteAsync(Request(), CancellationToken.None);
        result.Status.Should().Be(AppointmentProposalResultStatus.Unavailable);
        result.Proposal.Should().BeNull();
    }

    [Theory]
    [InlineData(true, false, AppointmentCommitStatus.Expired)]
    [InlineData(false, false, AppointmentCommitStatus.Stale)]
    public async Task Confirm_rejects_expired_or_stale_proposals(bool expired, bool available, AppointmentCommitStatus expected)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Store store = new();
        AppointmentProposal proposal = new(Guid.NewGuid(), Guid.NewGuid(), "practice", Slot(), null, now.AddMinutes(-20),
            expired ? now.AddMinutes(-1) : now.AddMinutes(5));
        await store.SaveProposalAsync(proposal, default);
        Mock<ISender> sender = Sender(Guid.NewGuid());

        AppointmentCommitResult result = await new ConfirmPatientAppointmentProposal(store, new Availability(available ? proposal.Slot : null), sender.Object, new FixedTimeProvider(now))
            .ExecuteAsync(proposal.Id, proposal.PatientId, proposal.PracticeId, default);

        result.Status.Should().Be(expected);
        sender.Verify(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_is_patient_scoped_and_duplicate_confirmation_returns_the_original_appointment()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Store store = new();
        AppointmentProposal proposal = new(Guid.NewGuid(), Guid.NewGuid(), "practice", Slot(), null, now, now.AddMinutes(5));
        await store.SaveProposalAsync(proposal, default);
        Guid appointmentId = Guid.NewGuid();
        Mock<ISender> sender = Sender(appointmentId);
        ConfirmPatientAppointmentProposal sut = new(store, new Availability(proposal.Slot), sender.Object, new FixedTimeProvider(now));

        (await sut.ExecuteAsync(proposal.Id, Guid.NewGuid(), proposal.PracticeId, default)).Status.Should().Be(AppointmentCommitStatus.Forbidden);
        (await sut.ExecuteAsync(proposal.Id, proposal.PatientId, proposal.PracticeId, default)).Status.Should().Be(AppointmentCommitStatus.Committed);
        AppointmentCommitResult duplicate = await sut.ExecuteAsync(proposal.Id, proposal.PatientId, proposal.PracticeId, default);

        duplicate.Status.Should().Be(AppointmentCommitStatus.AlreadyCommitted);
        duplicate.AppointmentId.Should().Be(appointmentId);
        sender.Verify(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_does_not_retry_an_in_progress_commit()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Store store = new();
        AppointmentProposal proposal = new(Guid.NewGuid(), Guid.NewGuid(), "practice", Slot(), null, now, now.AddMinutes(5));
        await store.SaveProposalAsync(proposal, default);
        store.SetCommit(proposal.Id, new(AppointmentCommitReceiptStatus.InProgress));
        Mock<ISender> sender = Sender(Guid.NewGuid());

        AppointmentCommitResult result = await new ConfirmPatientAppointmentProposal(
            store, new Availability(proposal.Slot), sender.Object, new FixedTimeProvider(now))
            .ExecuteAsync(proposal.Id, proposal.PatientId, proposal.PracticeId, default);

        result.Status.Should().Be(AppointmentCommitStatus.InProgress);
        sender.Verify(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PreparePatientAppointment Create(IPatientAppointmentAvailability availability, Store store) =>
        new(new CheckPatientAppointmentAvailability(availability), store, TimeProvider.System);
    private static PatientAppointmentRequest Request(string local = "2026-08-12T09:00", int? duration = 30, string? service = "Consultation") =>
        new(Guid.NewGuid(), "practice", local, duration, service, null, null, null, "Europe/London");
    private static AppointmentSlot Slot() => new(Guid.NewGuid(), new DateOnly(2026, 8, 12), new TimeOnly(9, 0), TimeSpan.FromMinutes(30), Guid.NewGuid(), "Consultation", Guid.NewGuid(), "Dr Smith", "Europe/London");
    private static Mock<ISender> Sender(Guid appointmentId)
    {
        Mock<ISender> sender = new();
        sender.Setup(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<Guid>.Success(appointmentId));
        return sender;
    }

    private sealed class Availability(AppointmentSlot? slot) : IPatientAppointmentAvailability
    {
        public Task<AppointmentSlot?> FindAvailableSlotAsync(PatientAppointmentRequest request, CancellationToken cancellationToken) => Task.FromResult(slot);
        public Task<bool> IsAvailableAsync(AppointmentSlot candidate, CancellationToken cancellationToken) => Task.FromResult(slot is not null);
    }
    private sealed class Store : IPatientAppointmentWorkflowStore
    {
        private readonly Dictionary<Guid, AppointmentProposal> proposals = [];
        private readonly Dictionary<Guid, AppointmentCommitReceipt> committed = [];
        public Task<AppointmentProposal?> GetProposalAsync(Guid id, string practiceId, CancellationToken cancellationToken) => Task.FromResult(proposals.GetValueOrDefault(id));
        public Task SaveProposalAsync(AppointmentProposal proposal, CancellationToken cancellationToken) { proposals.Add(proposal.Id, proposal); return Task.CompletedTask; }
        public Task<AppointmentCommitReceipt?> GetCommitReceiptAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken) => Task.FromResult(committed.GetValueOrDefault(proposalId));
        public Task<bool> TryClaimCommitAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken) => Task.FromResult(committed.TryAdd(proposalId, new(AppointmentCommitReceiptStatus.InProgress)));
        public Task<bool> CompleteCommitAsync(Guid proposalId, string practiceId, AppointmentCommitReceipt receipt, CancellationToken cancellationToken) { committed[proposalId] = receipt; return Task.FromResult(true); }
        public void SetCommit(Guid proposalId, AppointmentCommitReceipt receipt) => committed[proposalId] = receipt;
    }
    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
