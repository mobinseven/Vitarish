using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class PatientViewStatsLoader(ClinicDbContext dbContext)
{
    public async Task LoadAsync(IReadOnlyList<PatientView> patients, CancellationToken cancellationToken)
    {
        if (patients.Count == 0)
        {
            return;
        }

        Guid[] patientIds = [.. patients.Select(patient => patient.Id)];

        Dictionary<Guid, ProjectStats> statsByPatientId = await dbContext.ProjectStats
            .AsNoTracking()
            .Where(stat => patientIds.Contains(stat.ProjectId))
            .ToDictionaryAsync(stat => stat.ProjectId, cancellationToken);

        foreach (PatientView patient in patients)
        {
            patient.Stats = statsByPatientId.TryGetValue(patient.Id, out ProjectStats? stats)
                ? ToStatsReadDto(stats)
                : new StatsReadDto();
        }
    }

    private static StatsReadDto ToStatsReadDto(ProjectStats stats) => new()
    {
        State = stats.State,
        Procedures = stats.Procedures,
        Events = stats.Events,
        CommittedEvents = stats.CommittedEvents,
        EventItems = stats.EventItems,
        NewItems = stats.NewItems,
        Unread = stats.Unread,
        LastUpdate = stats.LastUpdate
    };
}
