
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.RequestIdentities.Domain.Entities;

namespace Vorn.Aaas.RequestIdentities.Infrastructure.Data;

public class RequestIdentityDbContext(DbContextOptions<RequestIdentityDbContext> options) : DbContext(options)
{
    public DbSet<RequestIdentity> RequestIdentities => Set<RequestIdentity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        _ = builder.Entity<RequestIdentity>(entity =>
        {
            _ = entity.HasKey(request => request.Id);
            _ = entity.Property(request => request.RequestId).HasMaxLength(64);
            _ = entity.Property(request => request.Authority).HasMaxLength(256).IsRequired();
            _ = entity.Property(request => request.ClientId).HasMaxLength(128).IsRequired();
            _ = entity.Property(request => request.OwnerId).HasMaxLength(128).IsRequired();
            _ = entity.Property(request => request.TenantId).HasMaxLength(256);
            _ = entity.Property(request => request.UserId).HasMaxLength(128);
            _ = entity.Property(request => request.ResourceId).HasMaxLength(512);
            _ = entity.Property(request => request.TraceId).HasMaxLength(64);
            _ = entity.Property(request => request.AdditionalInfo).HasMaxLength(2048);
            _ = entity.Property(request => request.CreatedAt).IsRequired();
            _ = entity.HasIndex(request => request.RequestId);
            _ = entity.HasIndex(request => request.Authority);
            _ = entity.HasIndex(request => request.ClientId);
            _ = entity.HasIndex(request => request.OwnerId);
            _ = entity.HasIndex(request => request.TenantId);
            _ = entity.HasIndex(request => request.UserId);
            _ = entity.HasIndex(request => request.ResourceId);
        });
    }
}
