using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Data;

public sealed class FilesDbContext : DbContext, IMultiTenantDbContext
{
    private readonly EntityInterceptor _interceptor;
    private readonly SessionContextInterceptor _sessionContext;
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

    internal DbSet<FileInformation> Files => Set<FileInformation>();

    public FilesDbContext(
        DbContextOptions<FilesDbContext> options,
        EntityInterceptor interceptor,
        SessionContextInterceptor sessionContext,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptor = interceptor;
        _sessionContext = sessionContext;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;

        // Enable multi-tenant tracking
        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
            .AddInterceptors(_interceptor)
            .AddInterceptors(_sessionContext);

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
        ConfigureTenantEntity(modelBuilder);

        _ = modelBuilder.Entity<FileInformation>()
            .Property(entity => entity.TenantId)
            .HasColumnName(nameof(FileInformation.Owner));

        _ = modelBuilder.Entity<FileInformation>()
            .HasIndex(entity => entity.Checksum);
    }

    private void ConfigureTenantEntity(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<FileInformation> entityBuilder = modelBuilder.Entity<FileInformation>();

        // Apply soft-delete filter and tenant filter with special RootTenant behavior
        // Note: Files module allows RootTenant to access all files across tenants
        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, "DeletedAt") == null &&
            (TenantInfo == null || 
             TenantInfo.Id == TenantDefaults.RootTenant || 
             e.TenantId == TenantInfo.Id));

        // Do NOT call IsMultiTenant() as we have custom tenant filtering logic above
    }
}
