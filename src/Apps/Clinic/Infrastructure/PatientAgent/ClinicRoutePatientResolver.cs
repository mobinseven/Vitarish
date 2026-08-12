using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.PatientAgent;

internal sealed class ClinicRoutePatientResolver(ClinicDbContext dbContext) : IPatientAgentRoutePatientResolver
{
    public Task<bool> IsAvailableAsync(string practiceId, Guid patientId, CancellationToken cancellationToken) =>
        dbContext.PatientViews.AsNoTracking().AnyAsync(patient =>
            patient.Id == patientId &&
            patient.TenantId == practiceId &&
            patient.DeletedAt == null &&
            !patient.Template,
            cancellationToken);
}
