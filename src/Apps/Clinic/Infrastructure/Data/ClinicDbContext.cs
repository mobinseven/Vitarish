using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Models;

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
    internal DbSet<SurgeryView> SurgeryViews => Set<SurgeryView>();
    internal DbSet<ProjectStats> ProjectStats => Set<ProjectStats>();
    public ClinicDbContext(
    DbContextOptions<ClinicDbContext> options,
    IRequestContext requestIdentity,
    IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _requestContext = requestIdentity;
        _httpContextAccessor = httpContextAccessor;

        this.EnforceMultiTenantOnTracking();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PatientView>(eb =>
        {
            _ = eb.ToView("mv_Patients", nameof(Clinic));
            _ = eb.HasNoKey();
            _ = eb.Property(patient => patient.TenantId).HasMaxLength(64);
            _ = eb.Property(patient => patient.UserIds).HasColumnType("text[]");
            _ = eb.Property(patient => patient.Tags).HasColumnType("text[]");
        });

        _ = modelBuilder.Entity<SurgeryView>(eb =>
        {
            _ = eb.ToView("mv_PatientSurgeries", nameof(Clinic));
            _ = eb.HasNoKey();
            _ = eb.Property(surgery => surgery.Data).HasColumnType("jsonb");
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _ = eb.Property(surgery => surgery.Data).HasConversion(
                    value => value.RootElement.GetRawText(),
                    value => JsonDocument.Parse(value));
            }
        });

        _ = modelBuilder.Entity<ProjectStats>(eb =>
        {
            _ = eb.ToView("vw_ProjectStats", nameof(Sea));
            _ = eb.HasNoKey();
        });
    }

}
