using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Modules.Base.Core.Platform.Idempotency;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Tests.AgentCapabilities;

public sealed class AppointmentSchedulingAgentCapabilityTests
{
    [Fact]
    public async Task Direct_transition_is_idempotent_for_the_trusted_tool_context()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        AgentSchedulingContext context = Context();
        Guid appointmentId = Guid.NewGuid();

        AgentSchedulingResult first = await sut.ConfirmAppointmentAsync(context, appointmentId, default);
        AgentSchedulingResult second = await sut.ConfirmAppointmentAsync(context, appointmentId, default);

        first.Status.Should().Be(AgentSchedulingStatus.Succeeded);
        second.Status.Should().Be(AgentSchedulingStatus.AlreadyApplied);
        reservations.Verify(x => x.ConfirmReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Terminal_receipt_failure_is_persisted_as_unknown_and_is_not_replayed()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());
        ReceiptStore<AgentSchedulingReceipt> receipts = new() { FailNextUpdate = true };
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object, receiptStore: receipts);
        Guid appointmentId = Guid.NewGuid();

        AgentSchedulingResult first = await sut.ConfirmAppointmentAsync(Context(), appointmentId, default);
        AgentSchedulingResult second = await sut.ConfirmAppointmentAsync(Context(), appointmentId, default);

        first.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        second.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        reservations.Verify(x => x.ConfirmReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
        receipts.Values.Should().ContainSingle().Which.Value.Status.Should().Be(AgentSchedulingReceiptStatus.UnknownOutcome);
    }

    [Fact]
    public async Task Invalid_context_is_rejected_before_manager_access()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);

        AgentSchedulingResult result = await sut.ConfirmAppointmentAsync(new("", "run", "call", "confirm"), Guid.NewGuid(), default);

        result.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        reservations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Prepared_shift_operation_rejects_stale_authoritative_state()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        Guid shiftId = Guid.NewGuid();
        ShiftReadDto draft = new() { Id = shiftId, Status = ShiftStatus.Draft };
        ShiftReadDto active = draft with { Status = ShiftStatus.Active };
        shifts.SetupSequence(x => x.GetAsync(shiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(draft))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(active));
        AppointmentSchedulingAgentCapability sut = Create(new Mock<IAccessControlledReservationManager>().Object, shifts.Object);

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareCloseShiftAsync(Context(), shiftId, default);
        AgentSchedulingResult executed = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.CloseShift, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        executed.Status.Should().Be(AgentSchedulingStatus.Conflict);
        shifts.Verify(x => x.CloseShiftAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AppointmentSchedulingAgentCapability Create(
        IAccessControlledReservationManager reservations,
        IAccessControlledShiftManager? shifts = null,
        ReceiptStore<AgentSchedulingReceipt>? receiptStore = null) =>
        new(
            reservations,
            shifts ?? new Mock<IAccessControlledShiftManager>().Object,
            new Mock<IShiftBookableDateQueryService>().Object,
            new Mock<IServiceManager>().Object,
            new Mock<IServiceProviderManager>().Object,
            new Mock<IServiceTypeManager>().Object,
            new ContextScope(),
            new ReceiptStore<AgentSchedulingProposal>(),
            receiptStore ?? new ReceiptStore<AgentSchedulingReceipt>(),
            TimeProvider.System);

    private static AgentSchedulingContext Context() => new("practice", "agent-run", "tool-call", "confirm");

    private sealed class ContextScope : IAgentSchedulingContextScope
    {
        public bool IsAllowed(string practiceId) => practiceId == "practice";
    }

    private sealed class ReceiptStore<T> : IOperationReceiptStore<T>
    {
        private readonly Dictionary<OperationReceiptKey, OperationReceipt<T>> receipts = [];
        public bool FailNextUpdate { get; set; }
        public IReadOnlyCollection<OperationReceipt<T>> Values => receipts.Values;

        public Task<OperationReceipt<T>?> GetAsync(OperationReceiptKey key, CancellationToken cancellationToken) =>
            Task.FromResult(receipts.GetValueOrDefault(key));

        public Task<bool> TryClaimAsync(OperationReceiptKey key, T value, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult(receipts.TryAdd(key, new(0, value)));

        public Task<bool> TryUpdateAsync(OperationReceiptKey key, long expectedRevision, T value, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            if (FailNextUpdate)
            {
                FailNextUpdate = false;
                return Task.FromResult(false);
            }

            if (!receipts.TryGetValue(key, out OperationReceipt<T>? current) || current.Revision != expectedRevision)
                return Task.FromResult(false);
            receipts[key] = new(expectedRevision + 1, value);
            return Task.FromResult(true);
        }
    }
}
