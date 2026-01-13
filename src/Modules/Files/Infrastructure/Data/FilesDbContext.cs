
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Vitarish.Files.Infrastructure.Data;

public sealed class FilesDbContext : MultiTenantDbContext
{
    private readonly EntityInterceptor interceptor;
    public FilesDbContext(
        IMultiTenantContextAccessor<TenantInfo> multiTenantContextAccessor,
        DbContextOptions<FilesDbContext> options,
        EntityInterceptor interceptor) : base(multiTenantContextAccessor, options)
    {
        this.interceptor = interceptor;
        TenantMismatchMode = TenantMismatchMode.Overwrite;
        TenantNotSetMode = TenantNotSetMode.Overwrite;
    }

    internal DbSet<FileInformation> Files => Set<FileInformation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(interceptor);
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<FileInformation>().HasQueryFilter(entity =>
            entity.DeletedAt == null &&
            TenantInfo != null &&
            (TenantInfo.Id == TenantDefaults.RootTenant || entity.TenantId == TenantInfo.Id));

        _ = modelBuilder.Entity<FileInformation>()
            .Property(entity => entity.TenantId)
            .HasColumnName(nameof(FileInformation.Owner));

        _ = modelBuilder.Entity<FileInformation>()
            .HasIndex(entity => entity.Checksum);

        base.OnModelCreating(modelBuilder);
    }
}
