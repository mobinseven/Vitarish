using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    public DbSet<SecurityAccessPolicyRecord> SecurityAccessPolicies => Set<SecurityAccessPolicyRecord>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<AuthenticationSessionNotice> AuthenticationSessionNotices => Set<AuthenticationSessionNotice>();
    public DbSet<AuthenticationAccessHistory> AuthenticationAccessHistories => Set<AuthenticationAccessHistory>();
    public DbSet<AuthenticationUserSessionPolicyRecord> AuthenticationUserSessionPolicies => Set<AuthenticationUserSessionPolicyRecord>();

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
            _ = entity.Property(policy => policy.MaximumConcurrentSessions).HasDefaultValue(1);
            _ = entity.Property(policy => policy.ConcurrentSessionsFunctionallyRequired).HasDefaultValue(false);
            _ = entity.Property(policy => policy.DefaultInactivityTimeoutMinutes).HasDefaultValue(30);
            _ = entity.Property(policy => policy.Version).IsConcurrencyToken();
        });
        _ = modelBuilder.Entity<AuthenticationSession>(entity =>
        {
            _ = entity.HasKey(session => session.Id);
            _ = entity.Property(session => session.UserId).IsRequired().HasMaxLength(450);
            _ = entity.Property(session => session.TenantId).IsRequired().HasMaxLength(64);
            _ = entity.Property(session => session.ClientIpAddress).HasMaxLength(45);
            _ = entity.HasIndex(session => new { session.UserId, session.TerminatedAt });
            _ = entity.HasIndex(session => session.LastActivityAt);
        });
        _ = modelBuilder.Entity<AuthenticationSessionNotice>(entity =>
        {
            _ = entity.HasKey(notice => notice.Id);
            _ = entity.Property(notice => notice.UserId).IsRequired().HasMaxLength(450);
            _ = entity.Property(notice => notice.NoticeType).IsRequired().HasMaxLength(128);
            _ = entity.HasIndex(notice => new { notice.SessionId, notice.AcknowledgedAt });
        });
        _ = modelBuilder.Entity<AuthenticationAccessHistory>(entity =>
        {
            _ = entity.HasKey(history => new { history.UserId, history.TenantId });
            _ = entity.Property(history => history.UserId).HasMaxLength(450);
            _ = entity.Property(history => history.TenantId).HasMaxLength(64);
        });
        _ = modelBuilder.Entity<AuthenticationUserSessionPolicyRecord>(entity =>
        {
            _ = entity.HasKey(policy => policy.UserId);
            _ = entity.Property(policy => policy.UserId).HasMaxLength(450);
            _ = entity.Property(policy => policy.Version).IsConcurrencyToken();
        });
    }
}

public sealed class AuthenticationUserSessionPolicyRecord
{
    public string UserId { get; set; } = string.Empty;
    public int? InactivityTimeoutMinutes { get; set; }
    public long Version { get; set; }
}
