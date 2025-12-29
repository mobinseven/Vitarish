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

        modelBuilder.Entity<AuthenticationClient>(entity =>
        {
            entity.HasKey(client => client.Id);
            entity.Property(client => client.Id).HasMaxLength(128).IsRequired();
            entity.Property(client => client.Url).HasMaxLength(2048).IsRequired();
            entity.Property(client => client.Title).HasMaxLength(200);
            entity.Property(client => client.Description).HasMaxLength(1024);
            entity.Property(client => client.LogoUrl).HasMaxLength(2048);
            entity.HasIndex(client => client.Url);
        });
    }
}
