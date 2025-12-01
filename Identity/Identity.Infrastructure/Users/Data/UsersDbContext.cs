using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Users.Data;

/// <summary>
/// EF Core database context for user management.
/// </summary>
public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>Users set.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Configures the model for the context.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(e => e.Id);

        modelBuilder.Entity<User>()
            .HasIndex(e => e.UserName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(e => e.PhoneNumber)
            .IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
