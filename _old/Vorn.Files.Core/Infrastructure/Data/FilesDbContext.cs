namespace Vorn.Files.Core.Infrastructure.Data;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vorn.Files.Core.Domain.Entities;

public sealed class FilesDbContext : MultiTenantDbContext
{
    private readonly EntityManagementInterceptor interceptor;

    public FilesDbContext(
        IMultiTenantContextAccessor<TenantInfo> multiTenantContextAccessor,
        DbContextOptions<FilesDbContext> options,
        EntityManagementInterceptor interceptor) : base(multiTenantContextAccessor, options)
    {
        this.interceptor = interceptor;
        TenantMismatchMode = TenantMismatchMode.Overwrite;
        TenantNotSetMode = TenantNotSetMode.Overwrite;
    }

    internal DbSet<FileInformation> FileInformations => Set<FileInformation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(interceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileInformation>().HasQueryFilter(entity =>
            entity.DeletedAt == null &&
            TenantInfo != null &&
            (TenantInfo.Id == Defaults.RootTenant || entity.TenantId == TenantInfo.Id));

        modelBuilder.Entity<FileInformation>()
            .Property(entity => entity.TenantId)
            .HasColumnName(nameof(FileInformation.Owner));

        modelBuilder.Entity<FileInformation>()
            .HasIndex(entity => entity.Checksum);

        if (Database.IsSqlite())
        {
            modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }

        base.OnModelCreating(modelBuilder);
    }
}
