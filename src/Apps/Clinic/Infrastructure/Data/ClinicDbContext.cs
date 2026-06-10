using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Data;

public class ClinicDbContext(IRequestContext requestContext, IHttpContextAccessor httpContextAccessor) : DbContext, Finbuckle.MultiTenant.EntityFrameworkCore.IMultiTenantDbContext
{
    public TenantMismatchMode TenantMismatchMode => throw new NotImplementedException();
    public TenantNotSetMode TenantNotSetMode => throw new NotImplementedException();
    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = requestContext.TenantId ?? (httpContextAccessor.HttpContext?.GetTenantId()) ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }
    internal DbSet<PatientView> PatientViews => Set<PatientView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        _ = modelBuilder.Entity<PatientView>(eb =>
        {
            _ = eb.ToView("mv_Patients", nameof(Sea));
            _ = eb.HasNoKey();
            _ = eb.Property(patient => patient.TenantId).HasMaxLength(64);
            _ = eb.IsMultiTenant();
        });
    }

}
