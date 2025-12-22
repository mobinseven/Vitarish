
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
    public DbSet<UserAccess> UsersAccess => Set<UserAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserAccess>(entity =>
        {

            entity.HasKey(access => new {
                access.ClientId,
                access.OwnerId,
                access.TenantId,
                access.UserId,
                access.ResourceId
            });

            entity.Property(access => access.ClientId)
                  .HasMaxLength(128)
                  .IsRequired();

            entity.Property(access => access.OwnerId)
                  .HasMaxLength(128)
                  .IsRequired();

            entity.Property(access => access.TenantId)
                  .HasMaxLength(256);

            entity.Property(access => access.UserId)
                  .HasMaxLength(128);

            entity.Property(access => access.ResourceId)
                  .HasMaxLength(512);

            entity.HasIndex(e => new { e.ClientId, e.OwnerId, e.TenantId, e.ResourceId })
                  .IncludeProperties(e => e.UserId);

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
