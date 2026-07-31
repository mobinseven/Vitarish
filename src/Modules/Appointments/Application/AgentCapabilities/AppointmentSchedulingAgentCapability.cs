using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core.Platform.Idempotency;
using Vitarish.Appointments.Application.Abstractions;

namespace Vitarish.Appointments.Application.AgentCapabilities;

public sealed record AgentSchedulingContext(
    string PracticeId,
    string AgentRunId,
    string ToolCallId,
    string ToolName,
    string? ActorId = null);

public interface IAgentSchedulingContextScope
{
    bool IsAllowed(string practiceId);
}

public sealed record AgentAppointmentListRequest(
    ReservationStatus? Status = null,
    Guid? ShiftId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? ReservingPartyId = null,
    int Skip = 0,
    int Take = 100);

public sealed record AgentAvailabilityRequest(DateOnly DateFrom, DateOnly DateTo, Guid? ShiftId = null);
public sealed record AgentShiftListRequest(ShiftStatus? Status = null, DayOfWeek? DayOfWeek = null, Guid? ServiceId = null, Guid? ServiceProviderId = null, int Skip = 0, int Take = 100);
public sealed record AgentOccurrenceRequest(Guid ShiftId, DateOnly DateFrom, DateOnly DateTo);
public sealed record AgentAppointmentInput(Guid ShiftId, DateOnly Date, string ReservingPartyId, TimeSpan? EstimatedSessionLength = null);
public sealed record AgentRescheduleInput(Guid ReservationId, Guid TargetShiftId, DateOnly TargetDate);
public sealed record AgentShiftInput(
    ShiftVisibility Visibility,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TimeZone,
    int TotalSeats,
    Guid? ServiceProviderId = null,
    Guid? ServiceId = null,
    int? BookingOpensBefore = null,
    int? BookingCloseBefore = null,
    Guid Id = default);

public enum AgentSchedulingStatus
{
    Succeeded,
    Prepared,
    AlreadyApplied,
    InProgress,
    ValidationFailure,
    NotFound,
    Forbidden,
    Conflict,
    Expired,
    Failed,
    UnknownOutcome
}

public sealed record AgentSchedulingResult<T>(AgentSchedulingStatus Status, T? Value = default, Guid? OperationId = null, string? Message = null);
public sealed record AgentSchedulingResult(AgentSchedulingStatus Status, Guid? OperationId = null, Guid? EntityId = null, string? Message = null);

public enum AgentSchedulingOperation
{
    BookAppointment,
    RescheduleAppointment,
    CancelAppointment,
    RejectAppointment,
    CreateShift,
    EditShift,
    CloseShift,
    CancelShift,
    ArchiveShift,
    ConfirmAppointment,
    PresentAppointment,
    CompleteAppointment,
    ActivateShift,
    PauseShift
}

public enum AgentSchedulingReceiptStatus { InProgress, Succeeded, Failed, UnknownOutcome }

public sealed record AgentSchedulingProposal
{
    public Guid Id { get; init; }
    public AgentSchedulingOperation Operation { get; init; }
    public AgentSchedulingContext Context { get; init; } = new("", "", "", "");
    public AgentAppointmentInput? Appointment { get; init; }
    public AgentRescheduleInput? Reschedule { get; init; }
    public AgentShiftInput? Shift { get; init; }
    public Guid? EntityId { get; init; }
    public ReservationStatus? ExpectedAppointmentStatus { get; init; }
    public ShiftStatus? ExpectedShiftStatus { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed record AgentSchedulingReceipt(AgentSchedulingReceiptStatus Status, Guid? EntityId = null, string? Message = null);

public interface IAppointmentSchedulingAgentCapability
{
    Task<AgentSchedulingResult<IReadOnlyList<ReservationReadDto>>> ListAppointmentsAsync(AgentSchedulingContext context, AgentAppointmentListRequest request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<int>> CountAppointmentsAsync(AgentSchedulingContext context, AgentAppointmentListRequest request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<ReservationReadDto>> GetAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<BookableShiftSlot>>> GetAvailabilityAsync(AgentSchedulingContext context, AgentAvailabilityRequest request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<ShiftReadDto>>> ListShiftsAsync(AgentSchedulingContext context, AgentShiftListRequest request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<ShiftReadDto>> GetShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<ShiftReservationReadDto>>> GetShiftOccurrencesAsync(AgentSchedulingContext context, AgentOccurrenceRequest request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<ServiceReadDto>>> ResolveServicesAsync(AgentSchedulingContext context, Guid? serviceTypeId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<ServiceTypeReadDto>>> ResolveServiceTypesAsync(AgentSchedulingContext context, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<IReadOnlyList<ServiceProviderReadDto>>> ResolveProvidersAsync(AgentSchedulingContext context, CancellationToken cancellationToken);

    Task<AgentSchedulingResult> ConfirmAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult> PresentAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult> CompleteAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult> ActivateShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult> PauseShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);

    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareBookAppointmentAsync(AgentSchedulingContext context, AgentAppointmentInput request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareRescheduleAppointmentAsync(AgentSchedulingContext context, AgentRescheduleInput request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCancelAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareRejectAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCreateShiftAsync(AgentSchedulingContext context, AgentShiftInput request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareEditShiftAsync(AgentSchedulingContext context, AgentShiftInput request, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCloseShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCancelShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareArchiveShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken);
    Task<AgentSchedulingResult> ExecutePreparedAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, CancellationToken cancellationToken);
}

public sealed class AppointmentSchedulingAgentCapability(
    IAccessControlledReservationManager reservations,
    IAccessControlledShiftManager shifts,
    IShiftBookableDateQueryService bookableSlots,
    IServiceManager services,
    IServiceProviderManager providers,
    IServiceTypeManager serviceTypes,
    IAgentSchedulingContextScope contextScope,
    IOperationReceiptStore<AgentSchedulingProposal> proposals,
    IOperationReceiptStore<AgentSchedulingReceipt> receipts,
    TimeProvider timeProvider) : IAppointmentSchedulingAgentCapability
{
    private static readonly TimeSpan ProposalLifetime = TimeSpan.FromMinutes(10);
    private const int MaximumReadRangeDays = 366;

    public async Task<AgentSchedulingResult<IReadOnlyList<ReservationReadDto>>> ListAppointmentsAsync(AgentSchedulingContext context, AgentAppointmentListRequest request, CancellationToken cancellationToken)
    {
        if (!ValidContext(context) || !ValidAppointmentPage(request))
            return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and bounded appointment page are required.");
        EntityResult<IReadOnlyList<ReservationReadDto>> result = await reservations.GetListAsync(new()
        {
            Status = request.Status, ShiftId = request.ShiftId, DateFrom = request.DateFrom, DateTo = request.DateTo,
            ReservingPartyId = request.ReservingPartyId, Skip = request.Skip, Take = request.Take
        }, cancellationToken);
        return Map(result);
    }

    public async Task<AgentSchedulingResult<int>> CountAppointmentsAsync(AgentSchedulingContext context, AgentAppointmentListRequest request, CancellationToken cancellationToken)
    {
        if (!ValidContext(context) || !ValidDateRange(request.DateFrom, request.DateTo)) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and date range are required.");
        return Map(await reservations.CountAsync(new() { Status = request.Status, ShiftId = request.ShiftId, DateFrom = request.DateFrom, DateTo = request.DateTo, ReservingPartyId = request.ReservingPartyId }, cancellationToken));
    }

    public async Task<AgentSchedulingResult<ReservationReadDto>> GetAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) =>
        !ValidContext(context) || appointmentId == Guid.Empty ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and appointment ID are required.") : Map(await reservations.GetAsync(appointmentId, cancellationToken));

    public async Task<AgentSchedulingResult<IReadOnlyList<BookableShiftSlot>>> GetAvailabilityAsync(AgentSchedulingContext context, AgentAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!ValidContext(context) || !ValidDateRange(request.DateFrom, request.DateTo)) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid date range is required.");
        return new(AgentSchedulingStatus.Succeeded, await bookableSlots.GetBookableSlotsAsync(ToShiftDescriptor(request.ShiftId), new() { DateFrom = request.DateFrom, DateTo = request.DateTo }, cancellationToken));
    }

    public async Task<AgentSchedulingResult<IReadOnlyList<ShiftReadDto>>> ListShiftsAsync(AgentSchedulingContext context, AgentShiftListRequest request, CancellationToken cancellationToken)
    {
        if (!ValidContext(context) || request.Skip < 0 || request.Take is <= 0 or > 500) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and bounded shift page are required.");
        return Map(await shifts.GetListAsync(new() { Status = request.Status, DayOfWeek = request.DayOfWeek, ServiceId = request.ServiceId, ServiceProviderId = request.ServiceProviderId, Skip = request.Skip, Take = request.Take }, cancellationToken));
    }

    public async Task<AgentSchedulingResult<ShiftReadDto>> GetShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) =>
        !ValidContext(context) || shiftId == Guid.Empty ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and shift ID are required.") : Map(await shifts.GetAsync(shiftId, cancellationToken));

    public async Task<AgentSchedulingResult<IReadOnlyList<ShiftReservationReadDto>>> GetShiftOccurrencesAsync(AgentSchedulingContext context, AgentOccurrenceRequest request, CancellationToken cancellationToken) =>
        !ValidContext(context) || request.ShiftId == Guid.Empty || !ValidDateRange(request.DateFrom, request.DateTo) ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid shift and date range are required.") : new(AgentSchedulingStatus.Succeeded, await bookableSlots.GetBookableDatesAsync(request.ShiftId, new() { DateFrom = request.DateFrom, DateTo = request.DateTo }, cancellationToken));

    public async Task<AgentSchedulingResult<IReadOnlyList<ServiceReadDto>>> ResolveServicesAsync(AgentSchedulingContext context, Guid? serviceTypeId, CancellationToken cancellationToken) =>
        !ValidContext(context) ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context is required.") : Map(await services.GetListAsync(new() { ServiceTypeId = serviceTypeId, Skip = 0, Take = 500 }, cancellationToken));

    public async Task<AgentSchedulingResult<IReadOnlyList<ServiceProviderReadDto>>> ResolveProvidersAsync(AgentSchedulingContext context, CancellationToken cancellationToken) =>
        !ValidContext(context) ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context is required.") : Map(await providers.GetListAsync(new() { Skip = 0, Take = 500 }, cancellationToken));

    public async Task<AgentSchedulingResult<IReadOnlyList<ServiceTypeReadDto>>> ResolveServiceTypesAsync(AgentSchedulingContext context, CancellationToken cancellationToken) =>
        !ValidContext(context) ? new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context is required.") : Map(await serviceTypes.GetListAsync(new() { Skip = 0, Take = 500 }, cancellationToken));

    public Task<AgentSchedulingResult> ConfirmAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) => DirectAsync(context, AgentSchedulingOperation.ConfirmAppointment, appointmentId, () => reservations.ConfirmReservationAsync(appointmentId, cancellationToken), cancellationToken);
    public Task<AgentSchedulingResult> PresentAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) => DirectAsync(context, AgentSchedulingOperation.PresentAppointment, appointmentId, () => reservations.PresentReservationAsync(appointmentId, cancellationToken), cancellationToken);
    public Task<AgentSchedulingResult> CompleteAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) => DirectAsync(context, AgentSchedulingOperation.CompleteAppointment, appointmentId, () => reservations.CompleteReservationAsync(appointmentId, cancellationToken), cancellationToken);
    public Task<AgentSchedulingResult> ActivateShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) => DirectAsync(context, AgentSchedulingOperation.ActivateShift, shiftId, () => shifts.ActivateShiftAsync(shiftId, cancellationToken), cancellationToken);
    public Task<AgentSchedulingResult> PauseShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) => DirectAsync(context, AgentSchedulingOperation.PauseShift, shiftId, () => shifts.PauseShiftAsync(shiftId, cancellationToken), cancellationToken);

    public Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareBookAppointmentAsync(AgentSchedulingContext context, AgentAppointmentInput request, CancellationToken cancellationToken) =>
        !ValidAppointmentInput(request)
            ? Task.FromResult(new AgentSchedulingResult<AgentSchedulingProposal>(AgentSchedulingStatus.ValidationFailure, Message: "A valid shift, date, and reserving party are required."))
            : PrepareAsync(context, AgentSchedulingOperation.BookAppointment, new() { Appointment = request }, cancellationToken);
    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareRescheduleAppointmentAsync(AgentSchedulingContext context, AgentRescheduleInput request, CancellationToken cancellationToken)
    {
        if (!ValidRescheduleInput(request)) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid reservation, target shift, and target date are required.");
        AgentSchedulingResult<ReservationReadDto> current = await GetAppointmentAsync(context, request.ReservationId, cancellationToken);
        return current.Value is null ? new(current.Status, Message: current.Message) : await PrepareAsync(context, AgentSchedulingOperation.RescheduleAppointment, new() { Reschedule = request, EntityId = request.ReservationId, ExpectedAppointmentStatus = current.Value.Status }, cancellationToken);
    }

    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCancelAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) => await PrepareAppointmentStatusAsync(context, AgentSchedulingOperation.CancelAppointment, appointmentId, cancellationToken);
    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareRejectAppointmentAsync(AgentSchedulingContext context, Guid appointmentId, CancellationToken cancellationToken) => await PrepareAppointmentStatusAsync(context, AgentSchedulingOperation.RejectAppointment, appointmentId, cancellationToken);
    public Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCreateShiftAsync(AgentSchedulingContext context, AgentShiftInput request, CancellationToken cancellationToken) => PrepareAsync(context, AgentSchedulingOperation.CreateShift, new() { Shift = request }, cancellationToken);
    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareEditShiftAsync(AgentSchedulingContext context, AgentShiftInput request, CancellationToken cancellationToken)
    {
        AgentSchedulingResult<ShiftReadDto> current = await GetShiftAsync(context, request.Id, cancellationToken);
        return current.Value is null ? new(current.Status, Message: current.Message) : await PrepareAsync(context, AgentSchedulingOperation.EditShift, new() { Shift = request, EntityId = request.Id, ExpectedShiftStatus = current.Value.Status }, cancellationToken);
    }

    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCloseShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) => await PrepareShiftStatusAsync(context, AgentSchedulingOperation.CloseShift, shiftId, cancellationToken);
    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareCancelShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) => await PrepareShiftStatusAsync(context, AgentSchedulingOperation.CancelShift, shiftId, cancellationToken);
    public async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareArchiveShiftAsync(AgentSchedulingContext context, Guid shiftId, CancellationToken cancellationToken) => await PrepareShiftStatusAsync(context, AgentSchedulingOperation.ArchiveShift, shiftId, cancellationToken);

    public async Task<AgentSchedulingResult> ExecutePreparedAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, CancellationToken cancellationToken)
    {
        if (!ValidContext(context)) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context is required.");
        OperationReceipt<AgentSchedulingProposal>? proposalReceipt = await proposals.GetAsync(Key("proposal", context, operation), cancellationToken);
        AgentSchedulingProposal? proposal = proposalReceipt?.Value;
        if (proposal is null) return new(AgentSchedulingStatus.NotFound, Message: "Prepared scheduling operation was not found or expired.");
        if (!proposal.Context.Equals(context)) return new(AgentSchedulingStatus.Forbidden, proposal.Id, Message: "Prepared operation belongs to another trusted context.");
        if (proposal.ExpiresAt <= timeProvider.GetUtcNow()) return new(AgentSchedulingStatus.Expired, proposal.Id, Message: "Prepared scheduling operation has expired.");
        if (proposal.ExpectedAppointmentStatus is { } expectedAppointment && proposal.EntityId is { } appointmentId)
        {
            AgentSchedulingResult<ReservationReadDto> current = await GetAppointmentAsync(context, appointmentId, cancellationToken);
            if (current.Value is null) return new(current.Status, proposal.Id, Message: current.Message);
            if (current.Value.Status != expectedAppointment) return new(AgentSchedulingStatus.Conflict, proposal.Id, Message: "Prepared appointment state is stale.");
        }
        if (proposal.ExpectedShiftStatus is { } expectedShift && proposal.EntityId is { } shiftId)
        {
            AgentSchedulingResult<ShiftReadDto> current = await GetShiftAsync(context, shiftId, cancellationToken);
            if (current.Value is null) return new(current.Status, proposal.Id, Message: current.Message);
            if (current.Value.Status != expectedShift) return new(AgentSchedulingStatus.Conflict, proposal.Id, Message: "Prepared shift state is stale.");
        }
        OperationReceipt<AgentSchedulingReceipt>? existing = await receipts.GetAsync(Key("outcome", context, operation), cancellationToken);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.Succeeded) return new(AgentSchedulingStatus.AlreadyApplied, proposal.Id, existing.Value.EntityId, existing.Value.Message);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.Failed) return new(AgentSchedulingStatus.Failed, proposal.Id, existing.Value.EntityId, existing.Value.Message);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.InProgress) return new(AgentSchedulingStatus.InProgress, proposal.Id, Message: "Scheduling operation is already in progress.");
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.UnknownOutcome) return new(AgentSchedulingStatus.UnknownOutcome, proposal.Id, Message: existing.Value.Message);
        if (!await receipts.TryClaimAsync(Key("outcome", context, operation), new(AgentSchedulingReceiptStatus.InProgress), ProposalLifetime, cancellationToken)) return new(AgentSchedulingStatus.InProgress, proposal.Id, Message: "Scheduling operation is already in progress.");

        AgentSchedulingResult result;
        try
        {
            switch (operation)
            {
                case AgentSchedulingOperation.BookAppointment when proposal.Appointment is not null:
                    EntityResult<ReservationReadDto> booking = await reservations.ReserveAsync(proposal.Appointment.ShiftId, proposal.Appointment.Date, proposal.Appointment.ReservingPartyId, proposal.Appointment.EstimatedSessionLength, cancellationToken);
                    result = await FinishAsync(context, operation, proposal.Id, booking.IsFailure ? Failure(booking.Errors) : new(AgentSchedulingStatus.Succeeded, proposal.Id, booking.Value?.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.RescheduleAppointment when proposal.Reschedule is not null:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await reservations.MoveReservationAsync(proposal.Reschedule.ReservationId, proposal.Reschedule.TargetShiftId, proposal.Reschedule.TargetDate, cancellationToken), proposal.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.CancelAppointment:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await reservations.CancelReservationAsync(proposal.EntityId!.Value, cancellationToken), proposal.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.RejectAppointment:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await reservations.RejectReservationAsync(proposal.EntityId!.Value, cancellationToken), proposal.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.CreateShift when proposal.Shift is not null:
                    EntityResult<ShiftReadDto> created = await shifts.AddAsync(ToWrite(proposal.Shift, ShiftStatus.Draft), cancellationToken);
                    result = await FinishAsync(context, operation, proposal.Id, created.IsFailure ? Failure(created.Errors, proposal.Id) : new(AgentSchedulingStatus.Succeeded, proposal.Id, created.Value?.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.EditShift when proposal.Shift is not null:
                    EntityResult<ShiftReadDto> currentShift = await shifts.GetAsync(proposal.EntityId!.Value, cancellationToken);
                    if (currentShift.IsFailure || currentShift.Value is null) return new(MapStatus(currentShift.Errors), proposal.Id, Message: currentShift.Errors.FirstOrDefault()?.Message);
                    EntityResult<ShiftReadDto> edited = await shifts.UpdateAsync(ToWrite(proposal.Shift, currentShift.Value.Status), cancellationToken);
                    result = await FinishAsync(context, operation, proposal.Id, edited.IsFailure ? Failure(edited.Errors, proposal.Id) : new(AgentSchedulingStatus.Succeeded, proposal.Id, edited.Value?.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.CloseShift:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await shifts.CloseShiftAsync(proposal.EntityId!.Value, cancellationToken), proposal.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.CancelShift:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await shifts.CancelShiftAsync(proposal.EntityId!.Value, cancellationToken), proposal.Id), cancellationToken);
                    break;
                case AgentSchedulingOperation.ArchiveShift:
                    result = await FinishAsync(context, operation, proposal.Id, await ToResult(await shifts.ArchiveShiftAsync(proposal.EntityId!.Value, cancellationToken), proposal.Id), cancellationToken);
                    break;
                default: return new(AgentSchedulingStatus.ValidationFailure, proposal.Id, Message: "Prepared operation payload is invalid.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await FinishAsync(context, operation, proposal.Id,
                new(AgentSchedulingStatus.UnknownOutcome, proposal.Id, Message: "Scheduling operation outcome could not be established."), cancellationToken);
        }
        return result;
    }

    private async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, AgentSchedulingProposal payload, CancellationToken cancellationToken)
    {
        if (!ValidContext(context)) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context is required.");
        OperationReceipt<AgentSchedulingProposal>? existing = await proposals.GetAsync(Key("proposal", context, operation), cancellationToken);
        if (existing is not null) return new(AgentSchedulingStatus.Prepared, existing.Value, existing.Value.Id);
        DateTimeOffset now = timeProvider.GetUtcNow();
        AgentSchedulingProposal proposal = payload with { Id = Guid.NewGuid(), Operation = operation, Context = context, CreatedAt = now, ExpiresAt = now.Add(ProposalLifetime) };
        if (!await proposals.TryClaimAsync(Key("proposal", context, operation), proposal, ProposalLifetime, cancellationToken))
            return await PrepareAsync(context, operation, payload, cancellationToken);
        return new(AgentSchedulingStatus.Prepared, proposal, proposal.Id);
    }

    private async Task<AgentSchedulingResult> DirectAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, Guid entityId, Func<Task<EntityResult>> action, CancellationToken cancellationToken)
    {
        if (!ValidContext(context) || entityId == Guid.Empty) return new(AgentSchedulingStatus.ValidationFailure, Message: "A valid context and entity ID are required.");
        OperationReceipt<AgentSchedulingReceipt>? existing = await receipts.GetAsync(Key("direct", context, operation, entityId), cancellationToken);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.Succeeded) return new(AgentSchedulingStatus.AlreadyApplied, entityId, existing.Value.EntityId);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.Failed) return new(AgentSchedulingStatus.Failed, entityId, existing.Value.EntityId, existing.Value.Message);
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.InProgress) return new(AgentSchedulingStatus.InProgress, entityId, Message: "Scheduling operation is already in progress.");
        if (existing?.Value.Status == AgentSchedulingReceiptStatus.UnknownOutcome) return new(AgentSchedulingStatus.UnknownOutcome, entityId, Message: existing.Value.Message);
        if (!await receipts.TryClaimAsync(Key("direct", context, operation, entityId), new(AgentSchedulingReceiptStatus.InProgress, entityId), ProposalLifetime, cancellationToken)) return new(AgentSchedulingStatus.InProgress, entityId);
        try
        {
            return await FinishDirectAsync(context, operation, entityId, await ToResult(await action(), entityId), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await FinishDirectAsync(context, operation, entityId,
                new(AgentSchedulingStatus.UnknownOutcome, entityId, Message: "Scheduling operation outcome could not be established."), cancellationToken);
        }
    }

    private async Task<AgentSchedulingResult> FinishAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, Guid operationId, AgentSchedulingResult result, CancellationToken cancellationToken, Guid? receiptEntityId = null)
    {
        AgentSchedulingReceiptStatus status = result.Status switch
        {
            AgentSchedulingStatus.Succeeded => AgentSchedulingReceiptStatus.Succeeded,
            AgentSchedulingStatus.UnknownOutcome => AgentSchedulingReceiptStatus.UnknownOutcome,
            _ => AgentSchedulingReceiptStatus.Failed
        };
        AgentSchedulingReceipt terminal = new(status, receiptEntityId ?? result.EntityId, result.Message);
        return await PersistTerminalAsync(Key("outcome", context, operation), operationId, terminal, result, cancellationToken);
    }

    private async Task<AgentSchedulingResult> FinishDirectAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, Guid entityId, AgentSchedulingResult result, CancellationToken cancellationToken)
    {
        AgentSchedulingReceiptStatus status = result.Status switch
        {
            AgentSchedulingStatus.Succeeded => AgentSchedulingReceiptStatus.Succeeded,
            AgentSchedulingStatus.UnknownOutcome => AgentSchedulingReceiptStatus.UnknownOutcome,
            _ => AgentSchedulingReceiptStatus.Failed
        };
        AgentSchedulingReceipt terminal = new(status, entityId, result.Message);
        return await PersistTerminalAsync(Key("direct", context, operation, entityId), entityId, terminal, result, cancellationToken);
    }

    private async Task<AgentSchedulingResult> PersistTerminalAsync(
        OperationReceiptKey key,
        Guid operationId,
        AgentSchedulingReceipt terminal,
        AgentSchedulingResult result,
        CancellationToken cancellationToken)
    {
        OperationReceipt<AgentSchedulingReceipt>? current = await receipts.GetAsync(key, cancellationToken);
        if (current is not null && await receipts.TryUpdateAsync(key, current.Revision, terminal, ProposalLifetime, cancellationToken))
        {
            current = await receipts.GetAsync(key, cancellationToken);
            if (current?.Value == terminal)
            {
                return result;
            }
        }
        else
        {
            current = await receipts.GetAsync(key, cancellationToken);
            if (current?.Value == terminal)
            {
                return result;
            }
        }

        const string message = "Scheduling operation outcome could not be recorded durably.";
        if (current?.Value.Status == AgentSchedulingReceiptStatus.InProgress)
        {
            AgentSchedulingReceipt unknown = new(AgentSchedulingReceiptStatus.UnknownOutcome, Message: message);
            _ = await receipts.TryUpdateAsync(key, current.Revision, unknown, ProposalLifetime, cancellationToken);
            OperationReceipt<AgentSchedulingReceipt>? persistedUnknown = await receipts.GetAsync(key, cancellationToken);
            if (persistedUnknown?.Value.Status == AgentSchedulingReceiptStatus.UnknownOutcome)
            {
                return new(AgentSchedulingStatus.UnknownOutcome, operationId, Message: persistedUnknown.Value.Message ?? message);
            }
        }

        return new(AgentSchedulingStatus.UnknownOutcome, operationId, Message: message);
    }

    private bool ValidContext(AgentSchedulingContext context) =>
        !string.IsNullOrWhiteSpace(context.PracticeId) && !string.IsNullOrWhiteSpace(context.AgentRunId) && !string.IsNullOrWhiteSpace(context.ToolCallId) && !string.IsNullOrWhiteSpace(context.ToolName) && contextScope.IsAllowed(context.PracticeId);

    private static bool ValidAppointmentPage(AgentAppointmentListRequest request) =>
        request.Skip >= 0 && request.Take is > 0 and <= 500 && ValidDateRange(request.DateFrom, request.DateTo);

    private static bool ValidAppointmentInput(AgentAppointmentInput request) =>
        request.ShiftId != Guid.Empty && request.Date != default && request.ReservingPartyId is not null && !string.IsNullOrWhiteSpace(request.ReservingPartyId);

    private static bool ValidRescheduleInput(AgentRescheduleInput request) =>
        request.ReservationId != Guid.Empty && request.TargetShiftId != Guid.Empty && request.TargetDate != default;

    private static bool ValidDateRange(DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (dateFrom is null && dateTo is null)
        {
            return true;
        }

        if (dateFrom is null || dateTo is null || dateFrom > dateTo)
        {
            return false;
        }

        return dateTo.Value.DayNumber - dateFrom.Value.DayNumber <= MaximumReadRangeDays;
    }

    private static ShiftDescriptorDto ToShiftDescriptor(Guid? shiftId) => shiftId is { } id
        ? new() { Id = id }
        : new();

    private static OperationReceiptKey Key(string scope, AgentSchedulingContext context, AgentSchedulingOperation operation, Guid? entityId = null) =>
        new($"appointments.agent.{scope}.{operation}.{entityId:D}", context.PracticeId, $"{context.AgentRunId}:{context.ToolCallId}:{context.ToolName}");

    private async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareAppointmentStatusAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, Guid appointmentId, CancellationToken cancellationToken)
    {
        AgentSchedulingResult<ReservationReadDto> current = await GetAppointmentAsync(context, appointmentId, cancellationToken);
        return current.Value is null ? new(current.Status, Message: current.Message) : await PrepareAsync(context, operation, new() { EntityId = appointmentId, ExpectedAppointmentStatus = current.Value.Status }, cancellationToken);
    }

    private async Task<AgentSchedulingResult<AgentSchedulingProposal>> PrepareShiftStatusAsync(AgentSchedulingContext context, AgentSchedulingOperation operation, Guid shiftId, CancellationToken cancellationToken)
    {
        AgentSchedulingResult<ShiftReadDto> current = await GetShiftAsync(context, shiftId, cancellationToken);
        return current.Value is null ? new(current.Status, Message: current.Message) : await PrepareAsync(context, operation, new() { EntityId = shiftId, ExpectedShiftStatus = current.Value.Status }, cancellationToken);
    }

    private static ShiftWriteDto ToWrite(AgentShiftInput input, ShiftStatus status) => new() { Id = input.Id, Status = status, Visibility = input.Visibility, DayOfWeek = input.DayOfWeek, StartTime = input.StartTime, EndTime = input.EndTime, TimeZone = input.TimeZone, TotalSeats = input.TotalSeats, ServiceProviderId = input.ServiceProviderId, ServiceId = input.ServiceId, BookingOpensBefore = input.BookingOpensBefore, BookingCloseBefore = input.BookingCloseBefore };

    private static AgentSchedulingResult<T> Map<T>(EntityResult<T> result) => result.IsFailure ? new(MapStatus(result.Errors), Message: result.Errors.FirstOrDefault()?.Message) : new(AgentSchedulingStatus.Succeeded, result.Value);
    private static AgentSchedulingResult<T> Failure<T>(IReadOnlyCollection<EntityError> errors, Guid? operationId = null) => new(MapStatus(errors), OperationId: operationId, Message: errors.FirstOrDefault()?.Message);
    private static AgentSchedulingResult Failure(IReadOnlyCollection<EntityError> errors, Guid? operationId = null) => new(MapStatus(errors), operationId, Message: errors.FirstOrDefault()?.Message);
    private static AgentSchedulingStatus MapStatus(IReadOnlyCollection<EntityError> errors) => errors.Any(e => e.Code == EntityErrorCode.NotFound) ? AgentSchedulingStatus.NotFound : errors.Any(e => e.Code is EntityErrorCode.Conflict or EntityErrorCode.Concurrency) ? AgentSchedulingStatus.Conflict : AgentSchedulingStatus.Failed;
    private static Task<AgentSchedulingResult> ToResult(EntityResult result, Guid operationId) => Task.FromResult(result.IsFailure ? Failure(result.Errors, operationId) : new AgentSchedulingResult(AgentSchedulingStatus.Succeeded, operationId));
}
