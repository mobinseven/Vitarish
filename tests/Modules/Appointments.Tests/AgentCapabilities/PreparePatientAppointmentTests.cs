using FluentAssertions;
using Vitarish.Appointments.Application.AgentCapabilities;

namespace Vitarish.Appointments.Tests.AgentCapabilities;

public sealed class PreparePatientAppointmentTests
{
    [Fact]
    public async Task Prepare_returns_exact_immutable_available_slot()
    {
        AppointmentSlot slot = Slot();
        Store store = new();
        PreparePatientAppointment sut = Create(new Availability(slot), store);

        AppointmentProposalResult result = await sut.ExecuteAsync(Request(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(AppointmentProposalResultStatus.Prepared);
        result.Proposal!.Slot.Should().Be(slot);
        (await store.GetProposalAsync(result.Proposal.Id, default)).Should().Be(result.Proposal);
    }

    [Theory]
    [InlineData("2026-10-25T01:30", 30, "Consultation", AppointmentProposalResultStatus.AmbiguousDate)]
    [InlineData("2026-08-12T09:00", null, "Consultation", AppointmentProposalResultStatus.MissingInformation)]
    [InlineData("2026-08-12T09:00", 30, null, AppointmentProposalResultStatus.MissingInformation)]
    public async Task Prepare_rejects_ambiguous_or_missing_material_information(string local, int? duration, string? service, AppointmentProposalResultStatus expected)
    {
        PreparePatientAppointment sut = Create(new Availability(Slot()), new Store());
        AppointmentProposalResult result = await sut.ExecuteAsync(Request(local, duration, service), TestContext.Current.CancellationToken);
        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task Prepare_reports_unavailable_without_saving_a_proposal()
    {
        AppointmentProposalResult result = await Create(new Availability(null), new Store())
            .ExecuteAsync(Request(), TestContext.Current.CancellationToken);
        result.Status.Should().Be(AppointmentProposalResultStatus.Unavailable);
        result.Proposal.Should().BeNull();
    }

    private static PreparePatientAppointment Create(IPatientAppointmentAvailability availability, Store store) =>
        new(new CheckPatientAppointmentAvailability(availability), store, TimeProvider.System);
    private static PatientAppointmentRequest Request(string local = "2026-08-12T09:00", int? duration = 30, string? service = "Consultation") =>
        new(Guid.NewGuid(), "practice", local, duration, service, null, null, null, null, "Europe/London");
    private static AppointmentSlot Slot() => new(Guid.NewGuid(), new DateOnly(2026, 8, 12), new TimeOnly(9, 0), TimeSpan.FromMinutes(30), Guid.NewGuid(), "Consultation", Guid.NewGuid(), "Dr Smith", "Europe/London");

    private sealed class Availability(AppointmentSlot? slot) : IPatientAppointmentAvailability
    {
        public Task<AppointmentSlot?> FindAvailableSlotAsync(PatientAppointmentRequest request, CancellationToken cancellationToken) => Task.FromResult(slot);
        public Task<bool> IsAvailableAsync(AppointmentSlot candidate, CancellationToken cancellationToken) => Task.FromResult(slot is not null);
    }
    private sealed class Store : IPatientAppointmentWorkflowStore
    {
        private readonly Dictionary<Guid, AppointmentProposal> proposals = [];
        public Task<AppointmentProposal?> GetProposalAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(proposals.GetValueOrDefault(id));
        public Task SaveProposalAsync(AppointmentProposal proposal, CancellationToken cancellationToken) { proposals.Add(proposal.Id, proposal); return Task.CompletedTask; }
        public Task<Guid?> GetCommittedAppointmentIdAsync(Guid proposalId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
        public Task SaveCommittedAppointmentIdAsync(Guid proposalId, Guid appointmentId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
