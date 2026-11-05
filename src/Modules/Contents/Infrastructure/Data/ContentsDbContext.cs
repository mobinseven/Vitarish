using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Contents.Infrastructure.Data;

public sealed class ContentsDbContext : DbContext, IMultiTenantDbContext
{
    private const string JsonColumnType = "jsonb";
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
            return tenantId is null
                ? null
                : (ITenantInfo)new TenantInfo
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
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<ContentType> ContentTypes => Set<ContentType>();
    public DbSet<ContentTypeSchema> ContentTypeSchemas => Set<ContentTypeSchema>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<ContentTerm> ContentTerms => Set<ContentTerm>();
    public DbSet<ContentRelation> ContentRelations => Set<ContentRelation>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ContentAsset> ContentAssets => Set<ContentAsset>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();
    internal DbSet<ContentPublicationOutboxRecord> ContentPublicationOutbox => Set<ContentPublicationOutboxRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
        .AddInterceptors(_interceptors);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureContentSnapshotsImmutable();
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureContentSnapshotsImmutable();
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Contents));

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<ContentItem>(modelBuilder);
        ConfigureTenantEntity<Site>(modelBuilder);
        ConfigureTenantEntity<ContentType>(modelBuilder);
        ConfigureTenantEntity<ContentTypeSchema>(modelBuilder);
        ConfigureTenantEntity<Term>(modelBuilder);
        ConfigureTenantEntity<ContentTerm>(modelBuilder);
        ConfigureTenantEntity<ContentRelation>(modelBuilder);
        ConfigureTenantEntity<Asset>(modelBuilder);
        ConfigureTenantEntity<ContentAsset>(modelBuilder);
        ConfigureTenantEntity<ContentRevision>(modelBuilder);

        _ = modelBuilder.Entity<Site>(entity =>
        {
            _ = entity.Property(e => e.Key).HasMaxLength(256).IsRequired();
            _ = entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            _ = entity.Property(e => e.DefaultLocale).HasMaxLength(32).IsRequired();
            _ = entity.Property(e => e.Locales)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, ContentsPersistenceJsonSerializerContext.Default.StringArray),
                    value => JsonSerializer.Deserialize(value, ContentsPersistenceJsonSerializerContext.Default.StringArray) ?? Array.Empty<string>())
                .HasColumnType(JsonColumnType);
            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(Site.Key)).IsUnique();
        });

        _ = modelBuilder.Entity<ContentType>(entity =>
        {
            _ = entity.Property(e => e.Key).HasMaxLength(256).IsRequired();
            _ = entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            _ = entity.Property(e => e.Description).HasMaxLength(4000);
            _ = entity.Property(e => e.CurrentSchemaVersion).IsRequired();
            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(ContentType.Key)).IsUnique();
        });

        _ = modelBuilder.Entity<ContentTypeSchema>(entity =>
        {
            _ = entity.Property(e => e.Fields)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, ContentsPersistenceJsonSerializerContext.Default.ContentFieldDefinitions),
                    value => JsonSerializer.Deserialize(value, ContentsPersistenceJsonSerializerContext.Default.ContentFieldDefinitions))
                .HasColumnType(JsonColumnType)
                .IsRequired();
            _ = entity.HasOne<ContentType>()
                .WithMany()
                .HasForeignKey(e => e.ContentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            _ = entity.HasIndex(e => new { e.ContentTypeId, e.Version }).IsUnique();
        });

        _ = modelBuilder.Entity<ContentItem>(entity =>
        {
            _ = entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Slug).HasMaxLength(256);
            _ = entity.Property(e => e.CanonicalPath).HasMaxLength(512);
            _ = entity.Property(e => e.Title).HasMaxLength(512);
            _ = entity.Property(e => e.Excerpt);
            _ = entity.Property(e => e.Locale).HasMaxLength(32);
            _ = entity.Property(e => e.Body)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<IReadOnlyList<ContentBodyBlock>>(v, JsonOptions) ?? new List<ContentBodyBlock>())
                .HasDefaultValueSql("'[]'");
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'");

            _ = entity.Property(e => e.Fields)
                .HasConversion(
                    value => SerializeJsonElement(value),
                    value => DeserializeJsonElement(value))
                .HasColumnType(JsonColumnType)
                .IsRequired(false);
            _ = entity.Property(e => e.Version)
                .IsConcurrencyToken()
                .IsRequired(false);

            _ = entity.HasOne<Site>()
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            _ = entity.HasOne<ContentType>()
                .WithMany()
                .HasForeignKey(e => e.ContentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            _ = entity.HasOne<ContentRevision>()
                .WithMany()
                .HasForeignKey(e => e.WorkingRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
            _ = entity.HasOne<ContentRevision>()
                .WithMany()
                .HasForeignKey(e => e.PublishedRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(ContentItem.CanonicalPath))
                .IsUnique()
                .HasFilter("\"SiteId\" IS NULL AND \"CanonicalPath\" IS NOT NULL AND \"CanonicalPath\" <> ''");
            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(ContentItem.Slug))
                .IsUnique()
                .HasFilter("\"SiteId\" IS NULL AND \"Slug\" IS NOT NULL AND \"Slug\" <> ''");
            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(ContentItem.SiteId), nameof(ContentItem.CanonicalPath))
                .IsUnique()
                .HasFilter("\"SiteId\" IS NOT NULL AND \"CanonicalPath\" IS NOT NULL AND \"CanonicalPath\" <> ''");

            _ = entity.HasIndex(e => e.PublishedAt);
            _ = entity.HasIndex(e => new { e.Type, e.Status, e.PublishedAt });
            _ = entity.HasIndex(e => e.AuthorId);
            _ = entity.HasIndex(e => e.Locale);
        });

        _ = modelBuilder.Entity<Term>(entity =>
        {
            _ = entity.Property(e => e.Kind).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Slug).HasMaxLength(256);
            _ = entity.Property(e => e.Name).HasMaxLength(256);
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'");

            _ = entity.HasOne<Term>()
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = entity.HasIndex(nameof(ContentPublicationOutboxRecord.TenantId), nameof(Term.Slug))
                .IsUnique()
                .HasFilter("\"Slug\" IS NOT NULL AND \"Slug\" <> ''");
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
                .HasDefaultValueSql("'{}'");

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
            _ = entity.Property(e => e.Url).HasMaxLength(2048);
            _ = entity.Property(e => e.AltText).HasMaxLength(1024);
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions) ?? new Dictionary<string, object?>())
                .HasDefaultValueSql("'{}'");

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
            _ = entity.Property(e => e.Snapshot);
            _ = entity.Property(e => e.Fields)
                .HasConversion(
                    value => SerializeJsonElement(value),
                    value => DeserializeJsonElement(value))
                .HasColumnType(JsonColumnType)
                .IsRequired(false);
            _ = entity.Property(e => e.Title).HasMaxLength(512);
            _ = entity.Property(e => e.Slug).HasMaxLength(256);
            _ = entity.Property(e => e.CanonicalPath).HasMaxLength(512);
            _ = entity.Property(e => e.Excerpt);
            _ = entity.Property(e => e.SchemaVersion).IsRequired(false);

            _ = entity.HasOne<ContentItem>()
                .WithMany()
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasIndex(e => new { e.ContentId, e.Version }).IsUnique();
        });

        _ = modelBuilder.Entity<ContentPublicationOutboxRecord>(entity =>
        {
            _ = entity.ToTable(ContentPublicationOutboxRecord.TableName);
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.TenantId).HasMaxLength(256).IsRequired();
            _ = entity.Property(e => e.Operation).HasConversion<string>().HasMaxLength(16).IsRequired();
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.Attempts).IsRequired();
            _ = entity.Property(e => e.NextAttemptAt).IsRequired();
            _ = entity.Property(e => e.LastError).HasMaxLength(1000);
            _ = entity.HasIndex(e => new { e.TenantId, e.CompletedAt, e.NextAttemptAt, e.LeaseUntil });
            _ = entity.IsMultiTenant();
        });
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTime?>(e, nameof(Entity.DeletedAt)) == null);

        _ = entityBuilder.IsMultiTenant();
    }

    private static string? SerializeJsonElement(JsonElement? value) => value is null
        ? null
        : JsonSerializer.Serialize(value.Value, ContentsPersistenceJsonSerializerContext.Default.JsonElement);

    private static JsonElement? DeserializeJsonElement(string? value)
    {
        if (value is null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private void EnsureContentSnapshotsImmutable()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<ContentRevision>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<ContentTypeSchema>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Content revisions and schemas are immutable and cannot be changed or deleted.");
        }
    }
}
