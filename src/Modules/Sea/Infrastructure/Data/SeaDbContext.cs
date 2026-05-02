using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Data;

public class SeaDbContext : DbContext, IMultiTenantDbContext
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

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<Event> Events => Set<Event>();
    //public DbSet<EventItem> EventItems => Set<EventItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<EventRole> EventRoles => Set<EventRole>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();

    public SeaDbContext(
        DbContextOptions<SeaDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestIdentity,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestIdentity;
        _httpContextAccessor = httpContextAccessor;

        // Enable multi-tenant tracking
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

        // Configure Finbuckle multi-tenant entities
        _ = modelBuilder.ConfigureMultiTenant();

        // Apply soft-delete filter first, then mark as multi-tenant
        // Finbuckle automatically merges its tenant filter with the existing filter
        ConfigureTenantEntity<Project>(modelBuilder);
        ConfigureTenantEntity<Procedure>(modelBuilder);
        ConfigureTenantEntity<Event>(modelBuilder);
        ConfigureTenantEntity<Attachment>(modelBuilder);
        ConfigureTenantEntity<Role>(modelBuilder);
        ConfigureTenantEntity<UserRole>(modelBuilder);
        ConfigureTenantEntity<EventRole>(modelBuilder);
        ConfigureTenantEntity<Receipt>(modelBuilder);
        ConfigureTenantEntity<ProjectUser>(modelBuilder);

        _ = modelBuilder.Entity<ProjectUser>(entity =>
        {
            _ = entity.HasIndex(pu => new { pu.ProjectId, pu.UserId });
        });

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }

        _ = modelBuilder.Entity<ProjectStats>(eb =>
        {
            _ = eb.ToView("vw_ProjectStats");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Project>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
            entity.Ignore(p => p.Stats);
            entity.Ignore(p => p.UserIds);
        });

        _ = modelBuilder.Entity<ProcedureStats>(eb =>
        {
            _ = eb.ToView("vw_ProcedureStats");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Procedure>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.Ignore(p => p.Stats);
        });

        _ = modelBuilder.Entity<EventStats>(eb =>
        {
            _ = eb.ToView("vw_EventStats");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Event>()
            .Ignore(e => e.Stats);
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        // Apply soft-delete filter first
        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, "DeletedAt") == null);

        // Mark as multi-tenant - Finbuckle will merge its tenant filter with the soft-delete filter
        _ = entityBuilder.IsMultiTenant();
    }
}
