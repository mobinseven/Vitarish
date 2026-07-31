using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Models;
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
    public async Task Direct_appointment_transition_allowlist_uses_distinct_idempotency_operations()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult.Success());
        reservations.Setup(x => x.PresentReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult.Success());
        reservations.Setup(x => x.CompleteReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult.Success());
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        Guid appointmentId = Guid.NewGuid();

        (await sut.ConfirmAppointmentAsync(Context(), appointmentId, default)).Status.Should().Be(AgentSchedulingStatus.Succeeded);
        (await sut.PresentAppointmentAsync(Context(), appointmentId, default)).Status.Should().Be(AgentSchedulingStatus.Succeeded);
        (await sut.CompleteAppointmentAsync(Context(), appointmentId, default)).Status.Should().Be(AgentSchedulingStatus.Succeeded);

        reservations.Verify(x => x.ConfirmReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
        reservations.Verify(x => x.PresentReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
        reservations.Verify(x => x.CompleteReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prepared_booking_executes_once_and_replays_receipt_without_rebooking()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        Guid appointmentId = Guid.NewGuid();
        reservations.Setup(x => x.ReserveAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new() { Id = appointmentId }));
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        AgentAppointmentInput input = new(Guid.NewGuid(), new(2026, 8, 12), "patient");

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareBookAppointmentAsync(Context(), input, default);
        AgentSchedulingResult first = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.BookAppointment, default);
        AgentSchedulingResult replay = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.BookAppointment, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        first.Status.Should().Be(AgentSchedulingStatus.Succeeded);
        replay.Status.Should().Be(AgentSchedulingStatus.AlreadyApplied);
        reservations.Verify(x => x.ReserveAsync(input.ShiftId, input.Date, input.ReservingPartyId, input.EstimatedSessionLength, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prepared_reschedule_rejects_stale_authoritative_appointment_state()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        Guid appointmentId = Guid.NewGuid();
        ReservationReadDto confirmed = new() { Id = appointmentId, Status = ReservationStatus.Confirmed };
        ReservationReadDto cancelled = confirmed with { Status = ReservationStatus.Cancelled };
        reservations.SetupSequence(x => x.GetAsync(appointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(confirmed))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(cancelled));
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareRescheduleAppointmentAsync(
            Context(), new(appointmentId, Guid.NewGuid(), new(2026, 8, 19)), default);
        AgentSchedulingResult executed = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.RescheduleAppointment, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        executed.Status.Should().Be(AgentSchedulingStatus.Conflict);
        reservations.Verify(x => x.MoveReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invalid_prepared_booking_is_rejected_before_receipt_claim()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);

        AgentSchedulingResult<AgentSchedulingProposal> result = await sut.PrepareBookAppointmentAsync(
            Context(), new(Guid.Empty, default, "patient"), default);

        result.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        reservations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Direct_transition_exception_is_unknown_and_is_not_replayed()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("booking dependency unavailable"));
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        Guid appointmentId = Guid.NewGuid();

        AgentSchedulingResult first = await sut.ConfirmAppointmentAsync(Context(), appointmentId, default);
        AgentSchedulingResult replay = await sut.ConfirmAppointmentAsync(Context(), appointmentId, default);

        first.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        replay.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        reservations.Verify(x => x.ConfirmReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Direct_transition_cancellation_is_unknown_and_is_not_replayed()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        Guid appointmentId = Guid.NewGuid();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        AgentSchedulingResult first = await sut.ConfirmAppointmentAsync(Context(), appointmentId, cancellation.Token);
        AgentSchedulingResult replay = await sut.ConfirmAppointmentAsync(Context(), appointmentId, default);

        first.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        replay.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        reservations.Verify(x => x.ConfirmReservationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prepared_operation_cancellation_is_unknown_and_is_not_replayed()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(x => x.ReserveAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);
        AgentAppointmentInput input = new(Guid.NewGuid(), new(2026, 8, 12), "patient");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareBookAppointmentAsync(Context(), input, default);
        AgentSchedulingResult first = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.BookAppointment, cancellation.Token);
        AgentSchedulingResult replay = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.BookAppointment, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        first.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        replay.Status.Should().Be(AgentSchedulingStatus.UnknownOutcome);
        reservations.Verify(x => x.ReserveAsync(input.ShiftId, input.Date, input.ReservingPartyId, input.EstimatedSessionLength, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Null_prepared_payloads_return_validation_failure()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object);

        AgentSchedulingResult<AgentSchedulingProposal> booking = await sut.PrepareBookAppointmentAsync(Context(), null!, default);
        AgentSchedulingResult<AgentSchedulingProposal> reschedule = await sut.PrepareRescheduleAppointmentAsync(Context(), null!, default);

        booking.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        reschedule.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        reservations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Direct_shift_transition_allowlist_is_idempotent()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        shifts.Setup(x => x.ActivateShiftAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult.Success());
        shifts.Setup(x => x.PauseShiftAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult.Success());
        AppointmentSchedulingAgentCapability sut = Create(shifts: shifts.Object);
        Guid shiftId = Guid.NewGuid();

        (await sut.ActivateShiftAsync(Context(), shiftId, default)).Status.Should().Be(AgentSchedulingStatus.Succeeded);
        (await sut.ActivateShiftAsync(Context(), shiftId, default)).Status.Should().Be(AgentSchedulingStatus.AlreadyApplied);
        (await sut.PauseShiftAsync(Context(), shiftId, default)).Status.Should().Be(AgentSchedulingStatus.Succeeded);
        (await sut.PauseShiftAsync(Context(), shiftId, default)).Status.Should().Be(AgentSchedulingStatus.AlreadyApplied);

        shifts.Verify(x => x.ActivateShiftAsync(shiftId, It.IsAny<CancellationToken>()), Times.Once);
        shifts.Verify(x => x.PauseShiftAsync(shiftId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prepared_shift_creation_is_draft_and_replays_without_creating_twice()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        Guid shiftId = Guid.NewGuid();
        shifts.Setup(x => x.AddAsync(It.IsAny<ShiftWriteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(new() { Id = shiftId, Status = ShiftStatus.Draft }));
        AppointmentSchedulingAgentCapability sut = Create(shifts: shifts.Object);
        AgentShiftInput input = ValidShift();

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareCreateShiftAsync(Context(), input, default);
        AgentSchedulingResult first = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.CreateShift, default);
        AgentSchedulingResult replay = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.CreateShift, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        first.Status.Should().Be(AgentSchedulingStatus.Succeeded);
        replay.Status.Should().Be(AgentSchedulingStatus.AlreadyApplied);
        shifts.Verify(x => x.AddAsync(It.Is<ShiftWriteDto>(write => write.Status == ShiftStatus.Draft && write.TotalSeats == input.TotalSeats), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prepared_shift_edit_rejects_stale_authoritative_state()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        Guid shiftId = Guid.NewGuid();
        ShiftReadDto draft = new() { Id = shiftId, Status = ShiftStatus.Draft };
        ShiftReadDto active = draft with { Status = ShiftStatus.Active };
        shifts.SetupSequence(x => x.GetAsync(shiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(draft))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(active));
        AppointmentSchedulingAgentCapability sut = Create(shifts: shifts.Object);

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareEditShiftAsync(Context(), ValidShift(shiftId), default);
        AgentSchedulingResult executed = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.EditShift, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        executed.Status.Should().Be(AgentSchedulingStatus.Conflict);
        shifts.Verify(x => x.UpdateAsync(It.IsAny<ShiftWriteDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invalid_shift_schedule_is_rejected_before_preparation()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        AppointmentSchedulingAgentCapability sut = Create(shifts: shifts.Object);

        AgentSchedulingResult<AgentSchedulingProposal> result = await sut.PrepareCreateShiftAsync(
            Context(), ValidShift() with { TotalSeats = 0, TimeZone = "not-a-time-zone" }, default);

        result.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        shifts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Invalid_timezone_is_reported_by_the_shift_manager_at_execution()
    {
        Mock<IAccessControlledShiftManager> shifts = new();
        shifts.Setup(x => x.AddAsync(It.IsAny<ShiftWriteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Failure(EntityError.Validation("Invalid timezone.")));
        AppointmentSchedulingAgentCapability sut = Create(shifts: shifts.Object);

        AgentSchedulingResult<AgentSchedulingProposal> prepared = await sut.PrepareCreateShiftAsync(
            Context(), ValidShift() with { TimeZone = "not-a-time-zone" }, default);
        AgentSchedulingResult executed = await sut.ExecutePreparedAsync(Context(), AgentSchedulingOperation.CreateShift, default);

        prepared.Status.Should().Be(AgentSchedulingStatus.Prepared);
        executed.Status.Should().Be(AgentSchedulingStatus.Failed);
        shifts.Verify(x => x.AddAsync(It.Is<ShiftWriteDto>(write => write.TimeZone == "not-a-time-zone"), It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task Availability_without_shift_filter_queries_all_shifts()
    {
        Mock<IShiftBookableDateQueryService> bookableSlots = new();
        DateOnly date = new(2026, 8, 3);
        bookableSlots.Setup(x => x.GetBookableSlotsAsync(
                It.Is<ShiftDescriptorDto>(descriptor => descriptor.Id is null),
                It.Is<ShiftDateRangeQuery>(range => range.DateFrom == date && range.DateTo == date),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        AppointmentSchedulingAgentCapability sut = Create(bookableSlots: bookableSlots.Object);

        AgentSchedulingResult<IReadOnlyList<BookableShiftSlot>> result = await sut.GetAvailabilityAsync(
            Context(), new AgentAvailabilityRequest(date, date), default);

        result.Status.Should().Be(AgentSchedulingStatus.Succeeded);
        bookableSlots.VerifyAll();
    }

    [Fact]
    public async Task Read_queries_delegate_to_access_controlled_appointments_and_shifts()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        Mock<IAccessControlledShiftManager> shifts = new();
        ReservationReadDto appointment = new() { Id = Guid.NewGuid(), Status = ReservationStatus.Confirmed };
        ShiftReadDto shift = new() { Id = Guid.NewGuid(), Status = ShiftStatus.Active };
        reservations.Setup(x => x.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success([appointment]));
        reservations.Setup(x => x.CountAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<int>.Success(1));
        shifts.Setup(x => x.GetListAsync(It.IsAny<ShiftDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ShiftReadDto>>.Success([shift]));
        AppointmentSchedulingAgentCapability sut = Create(reservations.Object, shifts.Object);

        AgentSchedulingResult<IReadOnlyList<ReservationReadDto>> appointments = await sut.ListAppointmentsAsync(
            Context(), new(Status: ReservationStatus.Confirmed, Take: 10), default);
        AgentSchedulingResult<int> count = await sut.CountAppointmentsAsync(Context(), new(), default);
        AgentSchedulingResult<IReadOnlyList<ShiftReadDto>> listedShifts = await sut.ListShiftsAsync(
            Context(), new(Status: ShiftStatus.Active, Take: 10), default);

        appointments.Value.Should().ContainSingle().Which.Id.Should().Be(appointment.Id);
        count.Value.Should().Be(1);
        listedShifts.Value.Should().ContainSingle().Which.Id.Should().Be(shift.Id);
    }

    [Fact]
    public async Task Invalid_read_range_is_rejected_before_dependency_access()
    {
        Mock<IShiftBookableDateQueryService> bookableSlots = new();
        AppointmentSchedulingAgentCapability sut = Create(bookableSlots: bookableSlots.Object);
        DateOnly from = new(2026, 1, 1);
        DateOnly to = from.AddDays(367);

        AgentSchedulingResult<IReadOnlyList<BookableShiftSlot>> result = await sut.GetAvailabilityAsync(
            Context(), new AgentAvailabilityRequest(from, to), default);

        result.Status.Should().Be(AgentSchedulingStatus.ValidationFailure);
        bookableSlots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detail_occurrence_and_reference_queries_return_their_scoped_results()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        Mock<IAccessControlledShiftManager> shifts = new();
        Mock<IShiftBookableDateQueryService> bookableSlots = new();
        Mock<IServiceManager> services = new();
        Mock<IServiceProviderManager> providers = new();
        Mock<IServiceTypeManager> serviceTypes = new();
        Guid appointmentId = Guid.NewGuid();
        Guid shiftId = Guid.NewGuid();
        DateOnly date = new(2026, 8, 3);
        reservations.Setup(x => x.GetAsync(appointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new() { Id = appointmentId }));
        shifts.Setup(x => x.GetAsync(shiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ShiftReadDto>.Success(new() { Id = shiftId }));
        bookableSlots.Setup(x => x.GetBookableDatesAsync(shiftId, It.IsAny<ShiftDateRangeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        services.Setup(x => x.GetListAsync(It.IsAny<ServiceDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ServiceReadDto>>.Success([]));
        providers.Setup(x => x.GetListAsync(It.IsAny<ServiceProviderDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ServiceProviderReadDto>>.Success([]));
        serviceTypes.Setup(x => x.GetListAsync(It.IsAny<ServiceTypeDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ServiceTypeReadDto>>.Success([]));
        AppointmentSchedulingAgentCapability sut = Create(
            reservations.Object, shifts.Object, bookableSlots.Object, services.Object, providers.Object, serviceTypes.Object);

        AgentSchedulingResult<ReservationReadDto> appointment = await sut.GetAppointmentAsync(Context(), appointmentId, default);
        AgentSchedulingResult<ShiftReadDto> shift = await sut.GetShiftAsync(Context(), shiftId, default);
        AgentSchedulingResult<IReadOnlyList<ShiftReservationReadDto>> occurrences = await sut.GetShiftOccurrencesAsync(
            Context(), new(shiftId, date, date), default);
        AgentSchedulingResult<IReadOnlyList<ServiceReadDto>> resolvedServices = await sut.ResolveServicesAsync(Context(), null, default);
        AgentSchedulingResult<IReadOnlyList<ServiceProviderReadDto>> resolvedProviders = await sut.ResolveProvidersAsync(Context(), default);
        AgentSchedulingResult<IReadOnlyList<ServiceTypeReadDto>> resolvedTypes = await sut.ResolveServiceTypesAsync(Context(), default);

        appointment.Value!.Id.Should().Be(appointmentId);
        shift.Value!.Id.Should().Be(shiftId);
        occurrences.Value.Should().BeEmpty();
        resolvedServices.Value.Should().BeEmpty();
        resolvedProviders.Value.Should().BeEmpty();
        resolvedTypes.Value.Should().BeEmpty();
    }

    private static AppointmentSchedulingAgentCapability Create(
        IAccessControlledReservationManager? reservations = null,
        IAccessControlledShiftManager? shifts = null,
        IShiftBookableDateQueryService? bookableSlots = null,
        IServiceManager? services = null,
        IServiceProviderManager? providers = null,
        IServiceTypeManager? serviceTypes = null,
        ReceiptStore<AgentSchedulingReceipt>? receiptStore = null) =>
        new(
            reservations ?? new Mock<IAccessControlledReservationManager>().Object,
            shifts ?? new Mock<IAccessControlledShiftManager>().Object,
            bookableSlots ?? new Mock<IShiftBookableDateQueryService>().Object,
            services ?? new Mock<IServiceManager>().Object,
            providers ?? new Mock<IServiceProviderManager>().Object,
            serviceTypes ?? new Mock<IServiceTypeManager>().Object,
            new ContextScope(),
            new ReceiptStore<AgentSchedulingProposal>(),
            receiptStore ?? new ReceiptStore<AgentSchedulingReceipt>(),
            TimeProvider.System);

    private static AgentSchedulingContext Context() => new("practice", "agent-run", "tool-call", "confirm");

    private static AgentShiftInput ValidShift(Guid id = default) => new(
        ShiftVisibility.Public,
        DayOfWeek.Monday,
        new TimeOnly(9, 0),
        new TimeOnly(17, 0),
        "Europe/London",
        4,
        Id: id);

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
