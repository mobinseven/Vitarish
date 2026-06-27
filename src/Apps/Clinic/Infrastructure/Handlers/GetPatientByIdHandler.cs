using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class GetPatientByIdHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    PatientViewStatsLoader statsLoader) : IRequestHandler<GetPatientByIdQuery, PatientView?>
{
    public async Task<PatientView?> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");

        PatientView? patient = await dbContext.PatientViews
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.TenantId == tenantId && candidate.Id == request.PatientId, cancellationToken);

        if (patient is not null)
        {
            await statsLoader.LoadAsync([patient], cancellationToken);
        }

        return patient;
    }
}
