using Vitarish.Entities.Server.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<User, IdentityRole, string>(options)
{
    /// <summary>
    /// WebAuthn/FIDO2 credentials (passkeys) for users.
    /// </summary>
    public DbSet<WebAuthnCredential> WebAuthnCredentials { get; set; } = null!;
    public DbSet<TemporaryAutoAuthLink> TemporaryAutoAuthLinks { get; set; } = null!;

    internal bool DeletionInProgress { get; set; }

    // Long-lived account locks span Application work. Their initiating boundary returns
    // conflicts instead of replaying an irreversible mutation after an ambiguous commit.
    internal UsersDbContext CreateAccountTransactionContext()
    {
        DbContextOptionsBuilder<UsersDbContext> builder = new(options);
        if (Database.IsRelational())
            builder.UseNpgsql(provider => provider.ExecutionStrategy(dependencies => new NonRetryingExecutionStrategy(dependencies)));
        return new(builder.Options);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        Database.CreateExecutionStrategy().ExecuteAsync(ct => SaveAccountChangesAsync(acceptAllChangesOnSuccess, ct), cancellationToken);

    private async Task<int> SaveAccountChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        if (DeletionInProgress || Model.FindEntityType(typeof(User))?.FindProperty(nameof(User.DeletedAt)) is null)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        ChangeTracker.DetectChanges();
        string[] subjects = ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => entry.Entity switch
            {
                User user when entry.State != EntityState.Added => user.Id,
                IdentityUserRole<string> role => role.UserId,
                IdentityUserClaim<string> claim => claim.UserId,
                IdentityUserLogin<string> login => login.UserId,
                IdentityUserToken<string> token => token.UserId,
                IdentityUserPasskey<string> passkey => passkey.UserId,
                WebAuthnCredential credential => credential.UserId,
                TemporaryAutoAuthLink link => link.UserId,
                _ => null
            }).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (subjects.Length == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await using IAsyncDisposable? gate = await UserAccountWriteLock.AcquireInMemoryAsync(this, cancellationToken);
        await using IDbContextTransaction? transaction = Database.IsRelational() && Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        foreach (string subject in subjects)
        {
            User? stored = await UserAccountWriteLock.ReadForUpdateAsync(this, subject, cancellationToken);
            if (stored is { IsDeleted: true })
                throw new DbUpdateConcurrencyException("A deleted account cannot be changed or receive credentials or memberships.");
            var tracked = ChangeTracker.Entries<User>().SingleOrDefault(entry => entry.Entity.Id == subject);
            if (tracked?.State == EntityState.Deleted)
                throw new DbUpdateConcurrencyException("Account IDs must be retained for historical attribution.");
            // Child-only writes participate in the same revision as deletion and stale account edits.
            if (stored is not null && tracked is null)
            {
                Attach(stored);
                stored.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            }
            else if (tracked is { State: EntityState.Unchanged })
                tracked.Entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
        int count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return count;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        _ = builder.ConfigureUtcDateTimeColumns();
        _ = builder.HasDefaultSchema(nameof(Users));

        // One sign-in namespace: email addresses and phone numbers are unique
        // across all accounts and cannot collide with another account's
        // Identity-managed NormalizedUserName.
        _ = builder.Entity<User>(entity =>
        {
            _ = entity.HasQueryFilter(user => user.DeletedAt == null);
            _ = entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasFilter("\"NormalizedEmail\" IS NOT NULL")
                .HasDatabaseName(UsersContactIdentifierIndexNames.EmailIndex);
            _ = entity.HasIndex(user => user.PhoneNumber)
                .IsUnique()
                .HasFilter("\"PhoneNumber\" IS NOT NULL")
                .HasDatabaseName(UsersContactIdentifierIndexNames.PhoneNumberIndex);
            // Existing accounts stay enabled when the additive status column is applied.
            _ = entity.Property(user => user.IsEnabled).HasDefaultValue(true);
        });

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
