using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Appointments.Infrastructure.Data;

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

public sealed class PatientAppointmentAvailability(AppointmentsDbContext dbContext) : IPatientAppointmentAvailability
{
    public async Task<AppointmentSlot?> FindAvailableSlotAsync(PatientAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(request.RequestedLocalDateTime, "yyyy-MM-dd'T'HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime requested)) return null;
        DateOnly date = DateOnly.FromDateTime(requested); TimeOnly time = TimeOnly.FromDateTime(requested); TimeSpan duration = TimeSpan.FromMinutes(request.DurationMinutes ?? 0);
        var candidates = await (from shift in dbContext.Shifts.AsNoTracking()
            join service in dbContext.Services.AsNoTracking() on shift.ServiceId equals service.Id
            join provider in dbContext.ServiceProviders.AsNoTracking() on shift.ServiceProviderId equals provider.Id into providers
            from provider in providers.DefaultIfEmpty()
            where shift.Status == ShiftStatus.Active && shift.DayOfWeek == date.DayOfWeek && shift.StartTime == time
                && (request.ServicePreference == null || service.Name == request.ServicePreference)
                && (request.AppointmentType == null || service.Name == request.AppointmentType)
                && (request.PractitionerPreference == null || provider != null && provider.Name == request.PractitionerPreference)
            select new { shift, service, provider }).ToListAsync(cancellationToken);
        foreach (var item in candidates)
        {
            AppointmentSlot slot = new(item.shift.Id, date, item.shift.StartTime, duration, item.service.Id, item.service.Name,
                item.provider == null ? null : item.provider.Id, item.provider == null ? null : item.provider.Name, item.shift.TimeZone);
            if (await IsAvailableAsync(slot, cancellationToken)) return slot;
        }
        return null;
    }
    public async Task<bool> IsAvailableAsync(AppointmentSlot slot, CancellationToken cancellationToken)
    {
        var shift = await dbContext.Shifts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == slot.ShiftId, cancellationToken);
        if (shift is null || shift.Status != ShiftStatus.Active || shift.DayOfWeek != slot.Date.DayOfWeek) return false;
        int count = await dbContext.Reservations.CountAsync(x => x.ShiftId == slot.ShiftId && x.Date == slot.Date && x.Status != ReservationStatus.Cancelled && x.Status != ReservationStatus.Expired && x.Status != ReservationStatus.Rejected && !x.IsHold, cancellationToken);
        return count < shift.TotalSeats;
    }
}
