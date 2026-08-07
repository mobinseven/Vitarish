using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Communications.Infrastructure.Data;

public class CommunicationsDbContext : DbContext, IMultiTenantDbContext
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

    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ReminderDeliveryAttempt> ReminderDeliveryAttempts => Set<ReminderDeliveryAttempt>();

    public CommunicationsDbContext(
        DbContextOptions<CommunicationsDbContext> options,
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
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Communications));

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<Reminder>(modelBuilder);
        ConfigureTenantEntity<ReminderDeliveryAttempt>(modelBuilder);
        _ = modelBuilder.Entity<ReminderDeliveryAttempt>().HasOne(a => a.Reminder).WithMany().HasForeignKey(a => a.ReminderId).OnDelete(DeleteBehavior.Cascade);
        _ = modelBuilder.Entity<ReminderDeliveryAttempt>().HasIndex(a => new { a.ReminderId, a.AttemptedAt });
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTime?>(e, "DeletedAt") == null);

        _ = entityBuilder.IsMultiTenant();
    }
}
