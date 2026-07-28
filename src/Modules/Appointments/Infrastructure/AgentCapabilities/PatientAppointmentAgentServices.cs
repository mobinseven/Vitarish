using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.AgentCapabilities;

public sealed class PatientAppointmentWorkflowStore : IPatientAppointmentWorkflowStore
{
    private readonly ConcurrentDictionary<Guid, AppointmentProposal> _proposals = new();
    private readonly ConcurrentDictionary<Guid, Guid> _committed = new();
    public Task<AppointmentProposal?> GetProposalAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_proposals.GetValueOrDefault(id));
    public Task SaveProposalAsync(AppointmentProposal proposal, CancellationToken cancellationToken) { if (!_proposals.TryAdd(proposal.Id, proposal)) throw new InvalidOperationException("Appointment proposal already exists."); return Task.CompletedTask; }
    public Task<Guid?> GetCommittedAppointmentIdAsync(Guid proposalId, CancellationToken cancellationToken) => Task.FromResult(_committed.TryGetValue(proposalId, out Guid id) ? (Guid?)id : null);
    public Task SaveCommittedAppointmentIdAsync(Guid proposalId, Guid appointmentId, CancellationToken cancellationToken) { _ = _committed.TryAdd(proposalId, appointmentId); return Task.CompletedTask; }
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
