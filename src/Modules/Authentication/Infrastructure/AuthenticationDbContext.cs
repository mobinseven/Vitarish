using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    public DbSet<SecurityAccessPolicyRecord> SecurityAccessPolicies => Set<SecurityAccessPolicyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(SecurityAccessPolicyRecord.SchemaName);
        _ = modelBuilder.Entity<SecurityAccessPolicyRecord>(entity =>
        {
            _ = entity.HasKey(policy => policy.Id);
            _ = entity.Property(policy => policy.Id).ValueGeneratedNever();
            _ = entity.Property(policy => policy.FailedAttemptResponse).IsRequired();
            _ = entity.Property(policy => policy.EnabledAuthenticationMechanisms).IsRequired().HasMaxLength(256);
            _ = entity.Property(policy => policy.PermittedPreAuthenticationActions).IsRequired().HasMaxLength(512);
            _ = entity.Property(policy => policy.Version).IsConcurrencyToken();
        });
    }
}
