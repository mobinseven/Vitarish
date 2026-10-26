using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Files.Infrastructure.Data;

public sealed class FilesDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? (_httpContextAccessor.HttpContext?.GetTenantId()) ?? throw new InvalidOperationException("Tenant ID cannot be recognized.");
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

    private string CurrentTenantId => TenantInfo?.Id ?? throw new InvalidOperationException("Tenant ID cannot be recognized.");

    internal DbSet<FileInformation> Files => Set<FileInformation>();
    internal DbSet<FileIntegrityRecord> FileIntegrityRecords => Set<FileIntegrityRecord>();

    public FilesDbContext(
        DbContextOptions<FilesDbContext> options,
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
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Files));

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

        _ = modelBuilder.Entity<FileInformation>()
            .Property(entity => entity.FileName)
            .HasMaxLength(FileTransferPolicy.MaximumFileNameLength);

        EntityTypeBuilder<FileIntegrityRecord> integrityRecord = modelBuilder.Entity<FileIntegrityRecord>();
        _ = integrityRecord.HasIndex(entity => entity.FileInformationId).IsUnique();
        _ = integrityRecord.HasOne<FileInformation>()
            .WithOne()
            .HasForeignKey<FileIntegrityRecord>(entity => entity.FileInformationId)
            .OnDelete(DeleteBehavior.ClientNoAction);
        _ = integrityRecord.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
    }

    private void ConfigureTenantEntity(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<FileInformation> entityBuilder = modelBuilder.Entity<FileInformation>();

        // Apply soft-delete and tenant filters. Cross-tenant access is a separate
        // platform-admin capability and must not be represented by a tenant value.
        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTime?>(e, "DeletedAt") == null &&
            e.TenantId == CurrentTenantId);

        // Do NOT call IsMultiTenant() as we have custom tenant filtering logic above
    }
}
