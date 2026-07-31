using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Models;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Appointments.Infrastructure.AgentCapabilities;

public sealed class PatientAppointmentWorkflowStore(
    IOperationReceiptStore<AppointmentProposal> proposals,
    IOperationReceiptStore<AppointmentCommitReceipt> commits) : IPatientAppointmentWorkflowStore
{
    private static readonly TimeSpan ProposalLifetime = TimeSpan.FromMinutes(10);

    public async Task<AppointmentProposal?> GetProposalAsync(Guid id, string practiceId, CancellationToken cancellationToken) =>
        (await proposals.GetAsync(Key("proposal", practiceId, id), cancellationToken))?.Value;

    public async Task SaveProposalAsync(AppointmentProposal proposal, CancellationToken cancellationToken)
    {
        if (!await proposals.TryClaimAsync(Key("proposal", proposal.PracticeId, proposal.Id), proposal,
            ProposalLifetime, cancellationToken))
            throw new InvalidOperationException("Appointment proposal already exists.");
    }

    public async Task<AppointmentCommitReceipt?> GetCommitReceiptAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken) =>
        (await commits.GetAsync(Key("commit", practiceId, proposalId), cancellationToken))?.Value;

    public Task<bool> TryClaimCommitAsync(Guid proposalId, string practiceId, CancellationToken cancellationToken) =>
        commits.TryClaimAsync(Key("commit", practiceId, proposalId),
            new(AppointmentCommitReceiptStatus.InProgress), ProposalLifetime, cancellationToken);

    public async Task<bool> CompleteCommitAsync(Guid proposalId, string practiceId, AppointmentCommitReceipt receipt, CancellationToken cancellationToken)
    {
        OperationReceipt<AppointmentCommitReceipt>? current = await commits.GetAsync(
            Key("commit", practiceId, proposalId), cancellationToken);
        return current?.Value.Status == AppointmentCommitReceiptStatus.InProgress && await commits.TryUpdateAsync(
            Key("commit", practiceId, proposalId), current.Revision, receipt, ProposalLifetime, cancellationToken);
    }

    private static OperationReceiptKey Key(string operation, string practiceId, Guid id) =>
        new($"appointments.patient-workflow.{operation}", practiceId, id.ToString("D"));
}

public sealed class PatientAppointmentAvailability(
    AppointmentsDbContext dbContext,
    IShiftBookableDateQueryService bookableSlots) : IPatientAppointmentAvailability
{
    public async Task<AppointmentSlot?> FindAvailableSlotAsync(PatientAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(request.RequestedLocalDateTime, "yyyy-MM-dd'T'HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime requested)) return null;
        DateOnly date = DateOnly.FromDateTime(requested);
        TimeOnly time = TimeOnly.FromDateTime(requested);
        TimeSpan duration = TimeSpan.FromMinutes(request.DurationMinutes ?? 0);
        var resources = await (from shift in dbContext.Shifts.AsNoTracking()
            join service in dbContext.Services.AsNoTracking() on shift.ServiceId equals service.Id
            join serviceType in dbContext.ServiceTypes.AsNoTracking() on service.ServiceTypeId equals serviceType.Id
            join provider in dbContext.ServiceProviders.AsNoTracking() on shift.ServiceProviderId equals provider.Id into providers
            from provider in providers.DefaultIfEmpty()
            where shift.TimeZone == request.TimeZone && shift.StartTime == time
                && (request.ServicePreference == null || service.Name == request.ServicePreference)
                && (request.AppointmentType == null || serviceType.Name == request.AppointmentType)
                && (request.PractitionerPreference == null || provider != null && provider.Name == request.PractitionerPreference)
            select new { ShiftId = shift.Id, ServiceId = service.Id, Service = service.Name,
                PractitionerId = provider == null ? (Guid?)null : provider.Id,
                Practitioner = provider == null ? null : provider.Name }).ToListAsync(cancellationToken);
        foreach (var resource in resources)
        {
            IReadOnlyList<BookableShiftSlot> slots = await bookableSlots.GetBookableSlotsAsync(
                new ShiftDescriptorDto { Id = resource.ShiftId },
                new ShiftDateRangeQuery { DateFrom = date, DateTo = date }, cancellationToken);
            if (slots.Any(candidate => candidate.Shift.Id == resource.ShiftId && candidate.Occurrence is { IsClosed: false, AvailableSeats: > 0 }))
                return new(resource.ShiftId, date, time, duration, resource.ServiceId, resource.Service,
                    resource.PractitionerId, resource.Practitioner, request.TimeZone);
        }
        return null;
    }
    public async Task<bool> IsAvailableAsync(AppointmentSlot slot, CancellationToken cancellationToken)
    {
        IReadOnlyList<BookableShiftSlot> slots = await bookableSlots.GetBookableSlotsAsync(
            new ShiftDescriptorDto { Id = slot.ShiftId },
            new ShiftDateRangeQuery { DateFrom = slot.Date, DateTo = slot.Date }, cancellationToken);
        return slots.Any(candidate => candidate.Shift.Id == slot.ShiftId && candidate.Occurrence is { IsClosed: false, AvailableSeats: > 0 });
    }
}
