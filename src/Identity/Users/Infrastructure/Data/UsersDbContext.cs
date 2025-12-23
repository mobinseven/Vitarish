
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User, IdentityRole, string>(options)
{
    /// <summary>
    /// WebAuthn/FIDO2 credentials (passkeys) for users.
    /// </summary>
    public DbSet<WebAuthnCredential> WebAuthnCredentials { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure WebAuthnCredential entity
        _ = builder.Entity<WebAuthnCredential>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.HasIndex(e => e.CredentialId).IsUnique();
            _ = entity.HasIndex(e => e.UserId).HasFilter("[IsActive] = 1");

            _ = entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
