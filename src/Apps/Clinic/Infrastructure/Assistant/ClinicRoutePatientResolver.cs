using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Assistant;

internal sealed class ClinicRoutePatientResolver(ClinicDbContext dbContext) : IClinicWorkspaceRoutePatientResolver
{
    public Task<bool> IsAvailableAsync(string practiceId, Guid patientId, CancellationToken cancellationToken) =>
        dbContext.PatientViews.AsNoTracking().AnyAsync(patient =>
            patient.Id == patientId &&
            patient.TenantId == practiceId &&
            patient.DeletedAt == null &&
            !patient.Template,
            cancellationToken);
}
