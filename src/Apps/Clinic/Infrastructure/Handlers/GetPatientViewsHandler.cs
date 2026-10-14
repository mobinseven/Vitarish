using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class GetPatientViewsHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    PatientViewQueryShape queryShape,
    PatientViewStatsLoader statsLoader,
    PatientSurgeryDatesLoader surgeryDatesLoader,
    TimeProvider timeProvider) : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientView>>
{
    private const int DefaultTake = 100;
    private const int MaximumTake = 1000;

    public async Task<IReadOnlyList<PatientView>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");

        IQueryable<PatientView> query = queryShape.CreateQuery(dbContext, request.Tags);
        query = queryShape.ApplyFilters(query, tenantId, request.Search, request.Template);
        query = queryShape.ApplySort(query, request.SortBy, request.Descending, dbContext, timeProvider.GetUtcNow());

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

        await statsLoader.LoadAsync(patients, cancellationToken);
        await surgeryDatesLoader.LoadAsync(patients, cancellationToken);

        return patients;
    }

}
