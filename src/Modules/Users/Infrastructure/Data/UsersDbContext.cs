
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
    public DbSet<TemporaryAutoAuthLink> TemporaryAutoAuthLinks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        _ = builder.HasDefaultSchema(nameof(Users));

        // Configure WebAuthnCredential entity
        _ = builder.Entity<WebAuthnCredential>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.HasIndex(e => e.CredentialId).IsUnique();
            _ = entity.HasIndex(e => e.UserId)
                      .HasFilter("\"IsActive\" = TRUE");

            _ = entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        _ = builder.Entity<TemporaryAutoAuthLink>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(128);
            _ = entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            _ = entity.Property(e => e.DestinationPath).IsRequired().HasMaxLength(2048);
            _ = entity.Property(e => e.Purpose).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.ExpiresAt).IsRequired();
            _ = entity.Property(e => e.ConsumedAt).IsRequired(false);
            _ = entity.Property(e => e.CreatedByDeviceHint).HasMaxLength(200);
            _ = entity.Property(e => e.ConsumedByIp).HasMaxLength(45);
            _ = entity.Property(e => e.ConsumedByUserAgent).HasMaxLength(512);

            _ = entity.HasIndex(e => e.CodeHash).IsUnique();
            _ = entity.HasIndex(e => e.UserId);
            _ = entity.HasIndex(e => e.ExpiresAt);
            _ = entity.HasIndex(e => e.ConsumedAt);

            _ = entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
