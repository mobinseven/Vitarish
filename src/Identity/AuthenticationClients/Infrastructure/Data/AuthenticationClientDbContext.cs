using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Infrastructure.Data;

/// <summary>
/// EF Core database context for authentication client management.
/// </summary>
public class AuthenticationClientDbContext(DbContextOptions<AuthenticationClientDbContext> options) : DbContext(options)
{
    /// <summary>Authentication client set.</summary>
    public DbSet<AuthenticationClient> AuthenticationClients => Set<AuthenticationClient>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<AuthenticationClient>(entity =>
        {
            _ = entity.HasKey(client => client.Id);
            _ = entity.Property(client => client.Id).HasMaxLength(128).IsRequired();
            _ = entity.Property(client => client.Url).HasMaxLength(2048).IsRequired();
            _ = entity.Property(client => client.Title).HasMaxLength(200);
            _ = entity.Property(client => client.Description).HasMaxLength(1024);
            _ = entity.Property(client => client.LogoUrl).HasMaxLength(2048);
            _ = entity.HasIndex(client => client.Url);
        });
    }
}
