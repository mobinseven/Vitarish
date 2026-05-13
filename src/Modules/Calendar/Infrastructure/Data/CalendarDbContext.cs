using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Calendar.Infrastructure.Data;

public class CalendarDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
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

    public DbSet<CalendarAccount> CalendarAccounts => Set<CalendarAccount>();
    public DbSet<CalendarObjectMapping> CalendarObjectMappings => Set<CalendarObjectMapping>();
    public DbSet<DomainCalendarRecord> DomainCalendarRecords => Set<DomainCalendarRecord>();
    public DbSet<CalendarParticipant> CalendarParticipants => Set<CalendarParticipant>();
    public DbSet<AvailabilityPolicy> AvailabilityPolicies => Set<AvailabilityPolicy>();
    public DbSet<OccurrenceProjection> OccurrenceProjections => Set<OccurrenceProjection>();
    public DbSet<CalendarSyncLedger> CalendarSyncLedgers => Set<CalendarSyncLedger>();

    public CalendarDbContext(
        DbContextOptions<CalendarDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;

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

        ConfigureTenantEntity<CalendarAccount>(modelBuilder);
        ConfigureTenantEntity<CalendarObjectMapping>(modelBuilder);
        ConfigureTenantEntity<DomainCalendarRecord>(modelBuilder);
        ConfigureTenantEntity<CalendarParticipant>(modelBuilder);
        ConfigureTenantEntity<AvailabilityPolicy>(modelBuilder);
        ConfigureTenantEntity<OccurrenceProjection>(modelBuilder);
        ConfigureTenantEntity<CalendarSyncLedger>(modelBuilder);

        _ = modelBuilder.Entity<DomainCalendarRecord>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, (JsonSerializerOptions?)null) ?? new())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
        });
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
