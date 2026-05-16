using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Infrastructure.Data;

public sealed class ContentsDbContext : DbContext, IMultiTenantDbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web);

    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;

    public ContentsDbContext(
        DbContextOptions<ContentsDbContext> options,
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
            if (tenantId is null)
            {
                return null;
            }

            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<ContentTerm> ContentTerms => Set<ContentTerm>();
    public DbSet<ContentRelation> ContentRelations => Set<ContentRelation>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ContentAsset> ContentAssets => Set<ContentAsset>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();

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

        ConfigureTenantEntity<ContentItem>(modelBuilder);
        ConfigureTenantEntity<Term>(modelBuilder);
        ConfigureTenantEntity<ContentTerm>(modelBuilder);
        ConfigureTenantEntity<ContentRelation>(modelBuilder);
        ConfigureTenantEntity<Asset>(modelBuilder);
        ConfigureTenantEntity<ContentAsset>(modelBuilder);
        ConfigureTenantEntity<ContentRevision>(modelBuilder);

        _ = modelBuilder.Entity<ContentItem>(entity =>
        {
            _ = entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Slug).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.CanonicalPath).HasColumnType("nvarchar(512)");
            _ = entity.Property(e => e.Title).HasColumnType("nvarchar(512)");
            _ = entity.Property(e => e.Excerpt).HasColumnType("nvarchar(max)");
            _ = entity.Property(e => e.Locale).HasColumnType("nvarchar(32)");
            _ = entity.Property(e => e.Body)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<IReadOnlyList<ContentBodyBlock>>(v, JsonOptions) ?? new List<ContentBodyBlock>())
                .HasDefaultValueSql("'[]'")
                .HasColumnType("nvarchar(max)");
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.HasIndex("TenantId", nameof(ContentItem.CanonicalPath))
                .IsUnique()
                .HasFilter("[CanonicalPath] IS NOT NULL AND [CanonicalPath] != ''");
            _ = entity.HasIndex("TenantId", nameof(ContentItem.Slug))
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL AND [Slug] != ''");

            _ = entity.HasIndex(e => e.PublishedAt);
            _ = entity.HasIndex(e => new { e.Type, e.Status, e.PublishedAt });
            _ = entity.HasIndex(e => e.AuthorId);
            _ = entity.HasIndex(e => e.Locale);
        });

        _ = modelBuilder.Entity<Term>(entity =>
        {
            _ = entity.Property(e => e.Kind).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Slug).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.Name).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.HasOne<Term>()
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = entity.HasIndex("TenantId", nameof(Term.Slug))
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL AND [Slug] != ''");
            _ = entity.HasIndex(e => new { e.Kind, e.ParentId });
        });

        _ = modelBuilder.Entity<ContentTerm>(entity =>
        {
            _ = entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(64);

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne<Term>()
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(e => new { e.ContentId, e.TermId, e.Role }).IsUnique();
            _ = entity.HasIndex(e => new { e.TermId, e.ContentId });
        });

        _ = modelBuilder.Entity<ContentRelation>(entity =>
        {
            _ = entity.Property(e => e.RelationType).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Weight).HasColumnType("decimal(9,4)");
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.FromContentId)
                .OnDelete(DeleteBehavior.NoAction);

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.ToContentId)
                .OnDelete(DeleteBehavior.NoAction);

            _ = entity.HasIndex(e => new { e.FromContentId, e.ToContentId, e.RelationType }).IsUnique();
            _ = entity.HasIndex(e => new { e.ToContentId, e.RelationType });
        });

        _ = modelBuilder.Entity<Asset>(entity =>
        {
            _ = entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Url).HasColumnType("nvarchar(2048)");
            _ = entity.Property(e => e.AltText).HasColumnType("nvarchar(1024)");
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");

            _ = entity.HasIndex(e => e.FileId);
            _ = entity.HasIndex(e => e.Url);
        });

        _ = modelBuilder.Entity<ContentAsset>(entity =>
        {
            _ = entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(64);

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(e => new { e.ContentId, e.AssetId, e.Role }).IsUnique();
            _ = entity.HasIndex(e => new { e.ContentId, e.Role, e.Position });
        });

        _ = modelBuilder.Entity<ContentRevision>(entity =>
        {
            _ = entity.Property(e => e.Snapshot).HasColumnType("nvarchar(max)");

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(e => new { e.ContentId, e.Version }).IsUnique();
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
