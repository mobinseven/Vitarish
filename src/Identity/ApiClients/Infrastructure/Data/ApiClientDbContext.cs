using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Infrastructure.Data;

/// <summary>
/// EF Core database context for API client management.
/// </summary>
public class ApiClientDbContext(DbContextOptions<ApiClientDbContext> options) : DbContext(options)
{
    /// <summary>API client set.</summary>
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<ApiClient>(entity =>
        {
            _ = entity.HasKey(client => client.Id);
            _ = entity.Property(client => client.Id).HasMaxLength(128).IsRequired();
            _ = entity.Property(client => client.Secret).HasMaxLength(256).IsRequired();
            _ = entity.Property(client => client.Scopes).HasMaxLength(1024).IsRequired();
            _ = entity.Property(client => client.HostUri).HasMaxLength(2048).IsRequired();
        });
    }
}
