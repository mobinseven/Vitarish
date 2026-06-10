using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Data;

public class ClinicDbContext : DbContext, Finbuckle.MultiTenant.EntityFrameworkCore.IMultiTenantDbContext
{
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public TenantMismatchMode TenantMismatchMode => throw new NotImplementedException();
    public TenantNotSetMode TenantNotSetMode => throw new NotImplementedException();
    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? (_httpContextAccessor.HttpContext?.GetTenantId()) ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }
    internal DbSet<PatientView> PatientViews => Set<PatientView>();
    public ClinicDbContext(
    DbContextOptions<ClinicDbContext> options,
    IRequestContext requestIdentity,
    IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _requestContext = requestIdentity;
        _httpContextAccessor = httpContextAccessor;

        this.EnforceMultiTenantOnTracking();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder) => _ = modelBuilder.Entity<PatientView>(eb =>
                                                                          {
                                                                              _ = eb.ToView("mv_Patients", nameof(Sea));
                                                                              _ = eb.HasNoKey();
                                                                              _ = eb.Property(patient => patient.TenantId).HasMaxLength(64);
                                                                              _ = eb.IsMultiTenant();
                                                                          });

}
