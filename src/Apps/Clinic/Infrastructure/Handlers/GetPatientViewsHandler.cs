using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Entities.Server.Infrastructure.Search;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class GetPatientViewsHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    EntitySearchPatternFactory searchPatternFactory) : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientView>>
{
    private const int DefaultTake = 100;
    private const int MaximumTake = 1000;

    public async Task<IReadOnlyList<PatientView>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");

        IQueryable<PatientView> query = dbContext.PatientViews
            .AsNoTracking()
            .Where(patient => patient.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            IReadOnlyCollection<string> patterns = searchPatternFactory.CreateSearchPatterns(request.Search);
            foreach (string pattern in patterns)
            {
                query = query.Where(patient =>
                    EF.Functions.ILike(patient.Name, pattern)
                    || EF.Functions.ILike(patient.Description, pattern)
                    || EF.Functions.ILike(patient.IdentificationCode!, pattern)
                    || EF.Functions.ILike(patient.FirstName!, pattern)
                    || EF.Functions.ILike(patient.LastName!, pattern)
                    || EF.Functions.ILike(patient.Phone!, pattern)
                    || EF.Functions.ILike(patient.Email!, pattern));
            }
        }

        query = query.OrderBy(patient => patient.LastName ?? patient.Name)
            .ThenBy(patient => patient.FirstName)
            .ThenBy(patient => patient.Name)
            .ThenBy(patient => patient.Id);

        if (request.Skip is > 0)
        {
            query = query.Skip(request.Skip.Value);
        }

        int take = request.Take is > 0
            ? Math.Min(request.Take.Value, MaximumTake)
            : DefaultTake;

        query = query.Take(take);

        return await query
            .ToListAsync(cancellationToken);
    }
}
