using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for Identity management.
/// Provides access to Users and Authentication Clients.
/// </summary>
public class IdentityDbContext : IdentityDbContext<User>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityDbContext"/>.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the authentication clients.
    /// </summary>
    public DbSet<AuthenticationClient> AuthenticationClients => Set<AuthenticationClient>();

    /// <summary>
    /// Configures the model for the database context.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuthenticationClient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Url).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.LogoUrl).HasMaxLength(512);
        });
    }
}
