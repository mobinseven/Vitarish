using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class GetPatientViewsHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    PatientViewQueryShape queryShape) : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientView>>
{
    private const int DefaultTake = 100;
    private const int MaximumTake = 1000;

    public async Task<IReadOnlyList<PatientView>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");

        IQueryable<PatientView> query = dbContext.PatientViews.AsNoTracking();
        query = queryShape.ApplyFilters(query, tenantId, request.Search, request.Template);
        query = queryShape.ApplySort(query, request.SortBy, request.Descending);

        if (request.Skip is > 0)
        {
            query = query.Skip(request.Skip.Value);
        }

        int take = request.Take is > 0
            ? Math.Min(request.Take.Value, MaximumTake)
            : DefaultTake;

        IReadOnlyList<PatientView> patients = await query
            .Take(take)
            .ToListAsync(cancellationToken);

        await PopulateStatsAsync(patients, cancellationToken);

        return patients;
    }

    private async Task PopulateStatsAsync(IReadOnlyList<PatientView> patients, CancellationToken cancellationToken)
    {
        if (patients.Count == 0)
        {
            return;
        }

        Guid[] patientIds = [.. patients.Select(patient => patient.Id)];
        var statsList = await dbContext.ProjectStats
            .AsNoTracking()
            .Where(stat => patientIds.Contains(stat.ProjectId))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, ProjectStats> statsByPatientId = statsList
            .GroupBy(stat => stat.ProjectId)
            .ToDictionary(g => g.Key, g => g.First());

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
