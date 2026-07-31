using Vitarish.Appointments.Application.Commands;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Application.AgentCapabilities;

public sealed record AppointmentSlot(
    Guid ShiftId, DateOnly Date, TimeOnly StartTime, TimeSpan Duration,
    Guid ServiceId, string Service, Guid? PractitionerId, string? Practitioner, string TimeZone);

public sealed record AppointmentProposal(
    Guid Id, Guid PatientId, string PracticeId, AppointmentSlot Slot, string? Description,
    DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public enum AppointmentProposalResultStatus { Prepared, Unavailable, MissingInformation, AmbiguousDate }
public sealed record AppointmentProposalResult(
    AppointmentProposalResultStatus Status, AppointmentProposal? Proposal = null, string? Message = null);

public enum AppointmentCommitStatus { Committed, AlreadyCommitted, InProgress, Expired, Stale, NotFound, Forbidden, Failed }
public sealed record AppointmentCommitResult(
    AppointmentCommitStatus Status, Guid? AppointmentId = null, AppointmentSlot? Slot = null, string? Message = null);

public enum AppointmentCommitReceiptStatus { InProgress, Committed, Failed }
public sealed record AppointmentCommitReceipt(AppointmentCommitReceiptStatus Status, Guid? AppointmentId = null);

public sealed record PatientAppointmentRequest(
    Guid PatientId, string PracticeId, string RequestedLocalDateTime, int? DurationMinutes,
    string? AppointmentType, string? ServicePreference, string? PractitionerPreference,
    string? Description, string TimeZone);

public interface IPatientAppointmentWorkflowStore
{
    Task<AppointmentProposal?> GetProposalAsync(Guid id, string practiceId, CancellationToken cancellationToken);
    Task SaveProposalAsync(AppointmentProposal proposal, CancellationToken cancellationToken);
    Task<AppointmentCommitReceipt?> GetCommitReceiptAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken);
    Task<bool> TryClaimCommitAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken);
    Task<bool> CompleteCommitAsync(Guid proposalId, string practiceId, AppointmentCommitReceipt receipt, CancellationToken cancellationToken);
}

public interface IPatientAppointmentAvailability
{
    Task<AppointmentSlot?> FindAvailableSlotAsync(PatientAppointmentRequest request, CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync(AppointmentSlot slot, CancellationToken cancellationToken);
}

public sealed class CheckPatientAppointmentAvailability(IPatientAppointmentAvailability availability)
{
    public Task<AppointmentSlot?> ExecuteAsync(PatientAppointmentRequest request, CancellationToken cancellationToken) =>
        availability.FindAvailableSlotAsync(request, cancellationToken);
}

public sealed class PreparePatientAppointment(
    CheckPatientAppointmentAvailability availability,
    IPatientAppointmentWorkflowStore store,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public async Task<AppointmentProposalResult> ExecuteAsync(PatientAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (request.DurationMinutes is null or <= 0 ||
            string.IsNullOrWhiteSpace(request.AppointmentType) && string.IsNullOrWhiteSpace(request.ServicePreference))
            return new(AppointmentProposalResultStatus.MissingInformation, Message: "Duration and appointment type or service are required.");

        if (!TryReadLocalDateTime(request.RequestedLocalDateTime, request.TimeZone, out _))
            return new(AppointmentProposalResultStatus.AmbiguousDate, Message: "Provide an unambiguous local date and time in ISO 8601 format.");

        AppointmentSlot? slot = await availability.ExecuteAsync(request, cancellationToken);
        if (slot is null)
            return new(AppointmentProposalResultStatus.Unavailable, Message: "No matching appointment slot is available.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        AppointmentProposal proposal = new(Guid.NewGuid(), request.PatientId, request.PracticeId, slot,
            request.Description, now, now.Add(Lifetime));
        await store.SaveProposalAsync(proposal, cancellationToken);
        return new(AppointmentProposalResultStatus.Prepared, proposal);
    }

    private static bool TryReadLocalDateTime(string value, string timeZoneId, out DateTimeOffset instant)
    {
        instant = default;
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm", null,
                System.Globalization.DateTimeStyles.None, out DateTime local)) return false;
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
        if (zone.IsAmbiguousTime(local) || zone.IsInvalidTime(local)) return false;
        instant = new DateTimeOffset(local, zone.GetUtcOffset(local));
        return true;
    }
}

public sealed class ConfirmPatientAppointmentProposal(
    IPatientAppointmentWorkflowStore store,
    IPatientAppointmentAvailability availability,
    ISender sender,
    TimeProvider timeProvider)
{
    public async Task<AppointmentCommitResult> ExecuteAsync(
        Guid proposalId, Guid patientId, string practiceId, CancellationToken cancellationToken)
    {
        AppointmentProposal? proposal = await store.GetProposalAsync(proposalId, practiceId, cancellationToken);
        if (proposal is null) return new(AppointmentCommitStatus.NotFound, Message: "Appointment proposal was not found.");
        if (proposal.PatientId != patientId || !string.Equals(proposal.PracticeId, practiceId, StringComparison.Ordinal))
            return new(AppointmentCommitStatus.Forbidden, Message: "Appointment proposal is outside the current patient scope.");

        AppointmentCommitReceipt? receipt = await store.GetCommitReceiptAsync(proposalId, practiceId, cancellationToken);
        if (receipt?.Status == AppointmentCommitReceiptStatus.Committed)
            return new(AppointmentCommitStatus.AlreadyCommitted, receipt.AppointmentId, proposal.Slot);
        if (receipt?.Status == AppointmentCommitReceiptStatus.Failed)
            return new(AppointmentCommitStatus.Failed, Slot: proposal.Slot, Message: "Appointment booking was not completed.");
        if (receipt?.Status == AppointmentCommitReceiptStatus.InProgress)
            return new(AppointmentCommitStatus.InProgress, Slot: proposal.Slot, Message: "Appointment booking is already in progress.");
        if (proposal.ExpiresAt <= timeProvider.GetUtcNow())
            return new(AppointmentCommitStatus.Expired, Slot: proposal.Slot, Message: "Appointment proposal has expired.");
        if (!await availability.IsAvailableAsync(proposal.Slot, cancellationToken))
            return new(AppointmentCommitStatus.Stale, Slot: proposal.Slot, Message: "The proposed slot is no longer available.");

        if (!await store.TryClaimCommitAsync(proposalId, practiceId, cancellationToken))
            return new(AppointmentCommitStatus.InProgress, Slot: proposal.Slot, Message: "Appointment booking is already in progress.");

        EntityResult<Guid> result = await sender.Send(new CreateReservationCommand(
            proposal.Slot.ShiftId, proposal.Slot.Date, patientId.ToString("D"),
            EstimatedSessionLength: proposal.Slot.Duration), cancellationToken);
        if (result.IsFailure || result.Value == Guid.Empty)
        {
            _ = await store.CompleteCommitAsync(proposalId, practiceId,
                new(AppointmentCommitReceiptStatus.Failed), cancellationToken);
            return new(AppointmentCommitStatus.Failed, Slot: proposal.Slot, Message: "Appointment booking was not completed.");
        }

        _ = await store.CompleteCommitAsync(proposalId, practiceId,
            new(AppointmentCommitReceiptStatus.Committed, result.Value), cancellationToken);
        return new(AppointmentCommitStatus.Committed, result.Value, proposal.Slot);
    }
}
