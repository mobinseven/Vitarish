using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class CountPatientViewsHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    PatientViewQueryShape queryShape) : IRequestHandler<CountPatientsQuery, int>
{
    public Task<int> Handle(CountPatientsQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to count patient views.");

        IQueryable<PatientView> query = dbContext.PatientViews.AsNoTracking();
        query = queryShape.ApplyFilters(query, tenantId, request.Search, request.Template);

        return query.CountAsync(cancellationToken);
    }
}
