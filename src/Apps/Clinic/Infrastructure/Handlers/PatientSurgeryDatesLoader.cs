using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Handlers;

/// <summary>Fills <see cref="PatientView.LastSurgeryAt"/> and <see cref="PatientView.NextSurgeryAt"/> for a page of patients.</summary>
internal sealed class PatientSurgeryDatesLoader(ClinicDbContext dbContext, TimeProvider timeProvider)
{
    public async Task LoadAsync(IReadOnlyList<PatientView> patients, CancellationToken cancellationToken)
    {
        if (patients.Count == 0)
        {
            return;
        }

        Guid[] patientIds = [.. patients.Select(patient => patient.Id)];
        DateTimeOffset now = timeProvider.GetUtcNow();

        Dictionary<Guid, DateTimeOffset> last = await PatientSurgeryDates.LastCandidates(dbContext, now)
            .AsNoTracking()
            .Where(surgery => patientIds.Contains(surgery.PatientId))
            .GroupBy(surgery => surgery.PatientId)
            .Select(group => new { PatientId = group.Key, At = group.Max(surgery => surgery.ScheduledAt) })
            .ToDictionaryAsync(item => item.PatientId, item => item.At, cancellationToken);
        Dictionary<Guid, DateTimeOffset> next = await PatientSurgeryDates.NextCandidates(dbContext, now)
            .AsNoTracking()
            .Where(surgery => patientIds.Contains(surgery.PatientId))
            .GroupBy(surgery => surgery.PatientId)
            .Select(group => new { PatientId = group.Key, At = group.Min(surgery => surgery.ScheduledAt) })
            .ToDictionaryAsync(item => item.PatientId, item => item.At, cancellationToken);

        foreach (PatientView patient in patients)
        {
            patient.LastSurgeryAt = last.TryGetValue(patient.Id, out DateTimeOffset lastAt) ? lastAt : null;
            patient.NextSurgeryAt = next.TryGetValue(patient.Id, out DateTimeOffset nextAt) ? nextAt : null;
        }
    }
}
