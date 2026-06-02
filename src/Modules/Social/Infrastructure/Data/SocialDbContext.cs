using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Vitarish.Social.Infrastructure.Data;

public sealed class SocialDbContext : DbContext, IMultiTenantDbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web);

    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;

    public SocialDbContext(
        DbContextOptions<SocialDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;
        this.EnforceMultiTenantOnTracking();
    }

    public ITenantInfo? TenantInfo
    {
        get
        {
            string? tenantId = _requestContext.TenantId;
            return tenantId is null
                ? null
                : (ITenantInfo)new TenantInfo
                {
                    Id = tenantId,
                    Identifier = tenantId,
                    Name = tenantId,
                };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<SocialConnection> SocialConnections => Set<SocialConnection>();
    public DbSet<SocialConnectionToken> SocialConnectionTokens => Set<SocialConnectionToken>();
    public DbSet<RawWebhookEvent> RawWebhookEvents => Set<RawWebhookEvent>();
    public DbSet<SocialEvent> SocialEvents => Set<SocialEvent>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
        .AddInterceptors(_interceptors);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<SocialConnection>(modelBuilder);
        ConfigureTenantEntity<SocialConnectionToken>(modelBuilder);
        ConfigureTenantEntity<RawWebhookEvent>(modelBuilder);
        ConfigureTenantEntity<SocialEvent>(modelBuilder);

        _ = modelBuilder.Entity<SocialConnection>(entity =>
        {
            _ = entity.Property(e => e.Provider).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.ExternalAccountId).HasMaxLength(256);
            _ = entity.Property(e => e.WebhookSecret).HasMaxLength(512);
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("text");

            _ = entity.HasIndex("TenantId", nameof(SocialConnection.Provider), nameof(SocialConnection.ExternalAccountId)).IsUnique();
        });

        _ = modelBuilder.Entity<SocialConnectionToken>(entity =>
        {
            _ = entity.Property(e => e.AccessToken).HasColumnType("text");
            _ = entity.Property(e => e.RefreshToken).HasColumnType("text");
            _ = entity.Property(e => e.Scopes).HasMaxLength(1024);

            _ = entity.HasIndex(e => e.ConnectionId).IsUnique();

            _ = entity.HasOne<SocialConnection>()
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        _ = modelBuilder.Entity<RawWebhookEvent>(entity =>
        {
            _ = entity.Property(e => e.Provider).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.VerificationStatus).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.ProcessingStatus).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.IdempotencyKey).HasMaxLength(256);
            _ = entity.Property(e => e.RawHeaders).HasColumnType("text");
            _ = entity.Property(e => e.RawBody).HasColumnType("text");
            _ = entity.Property(e => e.ProcessingError).HasColumnType("text");

            _ = entity.HasIndex("TenantId", nameof(RawWebhookEvent.IdempotencyKey)).IsUnique();
            _ = entity.HasIndex(e => new { e.Provider, e.ReceivedAt });

            _ = entity.HasOne<SocialConnection>()
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        _ = modelBuilder.Entity<SocialEvent>(entity =>
        {
            _ = entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.NormalizedPayload).HasColumnType("text");

            _ = entity.HasIndex(e => new { e.ConnectionId, e.EventType, e.OccurredAt });
            _ = entity.HasIndex(e => e.RawEventId).IsUnique();

            _ = entity.HasOne<SocialConnection>()
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne<RawWebhookEvent>()
                .WithMany()
                .HasForeignKey(e => e.RawEventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, nameof(Entity.DeletedAt)) == null);

        _ = entityBuilder.IsMultiTenant();
    }
}
