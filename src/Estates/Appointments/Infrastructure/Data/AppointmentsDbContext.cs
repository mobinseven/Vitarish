using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Data;

public class AppointmentsDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

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

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public AppointmentsDbContext(
        DbContextOptions<AppointmentsDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;

        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
            .AddInterceptors(_interceptors);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<Appointment>(modelBuilder);

        _ = modelBuilder.Entity<Appointment>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
        });

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, "DeletedAt") == null);

        _ = entityBuilder.IsMultiTenant();
    }
}
