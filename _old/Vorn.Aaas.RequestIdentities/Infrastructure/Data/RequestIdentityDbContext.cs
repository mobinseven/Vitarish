namespace Vorn.Aaas.RequestIdentities.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.RequestIdentities.Domain.Entities;

public class RequestIdentityDbContext(DbContextOptions<RequestIdentityDbContext> options) : DbContext(options)
{
    public DbSet<RequestIdentity> RequestIdentities => Set<RequestIdentity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RequestIdentity>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.Property(request => request.RequestId).HasMaxLength(64);
            entity.Property(request => request.Authority).HasMaxLength(256).IsRequired();
            entity.Property(request => request.ClientId).HasMaxLength(128).IsRequired();
            entity.Property(request => request.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(request => request.TenantId).HasMaxLength(256);
            entity.Property(request => request.UserId).HasMaxLength(128);
            entity.Property(request => request.ResourceId).HasMaxLength(512);
            entity.Property(request => request.TraceId).HasMaxLength(64);
            entity.Property(request => request.AdditionalInfo).HasMaxLength(2048);
            entity.Property(request => request.CreatedAt).IsRequired();
            entity.HasIndex(request => request.RequestId);
            entity.HasIndex(request => request.Authority);
            entity.HasIndex(request => request.ClientId);
            entity.HasIndex(request => request.OwnerId);
            entity.HasIndex(request => request.TenantId);
            entity.HasIndex(request => request.UserId);
            entity.HasIndex(request => request.ResourceId);
        });
    }
}
