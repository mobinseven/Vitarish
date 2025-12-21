
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User>(options)
{
    /// <summary>
    /// WebAuthn/FIDO2 credentials (passkeys) for users.
    /// </summary>
    public DbSet<WebAuthnCredential> WebAuthnCredentials { get; set; } = null!;
    public DbSet<Access> Accesses => Set<Access>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        _ = builder.Entity<Access>(entity =>
        {
            _ = entity.HasKey(access => access.Id);
            _ = entity.Property(access => access.ClientId).HasMaxLength(128).IsRequired();
            _ = entity.Property(access => access.OwnerId).HasMaxLength(128).IsRequired();
            _ = entity.Property(access => access.TenantId).HasMaxLength(256);
            _ = entity.Property(access => access.UserId).HasMaxLength(128);
            _ = entity.Property(access => access.ResourceId).HasMaxLength(512);
            _ = entity.HasIndex(access => access.ClientId);
            _ = entity.HasIndex(access => access.OwnerId);
            _ = entity.HasIndex(access => access.TenantId);
            _ = entity.HasIndex(access => access.UserId);
            _ = entity.HasIndex(access => access.ResourceId);
        });
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
