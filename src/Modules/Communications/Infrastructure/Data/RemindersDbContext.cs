using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Communications.Infrastructure.Data;

public class RemindersDbContext : DbContext, IMultiTenantDbContext
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

    public DbSet<AuditEntry> CentralAuditEntries => Set<AuditEntry>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

    public RemindersDbContext(
        DbContextOptions<RemindersDbContext> options,
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

        ConfigureTenantEntity<AuditEntry>(modelBuilder);
        ConfigureTenantEntity<Reminder>(modelBuilder);
        ConfigureAuditEntry(modelBuilder);

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

    private static void ConfigureAuditEntry(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<AuditEntry>(entity =>
        {
            _ = entity.ToTable("AuditEntries");
            _ = entity.HasIndex("TenantId", nameof(AuditEntry.OccurredAt));
            _ = entity.HasIndex(nameof(AuditEntry.EntityType), nameof(AuditEntry.EntityId), nameof(AuditEntry.OccurredAt));
            _ = entity.HasIndex(nameof(AuditEntry.ActorId), nameof(AuditEntry.OccurredAt));
        });
    }
}
