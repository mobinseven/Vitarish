using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class GetPatientByIdentificationCodeHandler(
    ClinicDbContext dbContext,
    IRequestContext requestContext,
    PatientViewStatsLoader statsLoader) : IRequestHandler<GetPatientByIdentificationCodeQuery, PatientView?>
{
    public async Task<PatientView?> Handle(GetPatientByIdentificationCodeQuery request, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID is required to query patient views.");
        string identificationCode = request.IdentificationCode.Trim();

        PatientView? patient = await dbContext.PatientViews
            .AsNoTracking()
            .Where(candidate => candidate.TenantId == tenantId && candidate.IdentificationCode == identificationCode)
            .OrderBy(candidate => candidate.LastName ?? candidate.Name)
            .ThenBy(candidate => candidate.FirstName)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is not null)
        {
            await statsLoader.LoadAsync([patient], cancellationToken);
        }

        return patient;
    }
}
