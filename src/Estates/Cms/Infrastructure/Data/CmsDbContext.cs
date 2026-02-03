using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Cms.Infrastructure.Data;

public class CmsDbContext : DbContext, IMultiTenantDbContext
{
    private readonly EntityInterceptor _interceptor;
    private readonly SessionContextInterceptor _sessionContext;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? (_httpContextAccessor.HttpContext?.GetTenantId()) ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
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

    public DbSet<ContentItem> Contents => Set<ContentItem>();
    public DbSet<ContentComment> Comments => Set<ContentComment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<ContentCategory> ContentCategories => Set<ContentCategory>();

    public CmsDbContext(
        DbContextOptions<CmsDbContext> options,
        EntityInterceptor interceptor,
        SessionContextInterceptor sessionContext,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptor = interceptor;
        _sessionContext = sessionContext;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;

        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
        .AddInterceptors(_interceptor)
        .AddInterceptors(_sessionContext);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<ContentItem>(modelBuilder);
        ConfigureTenantEntity<ContentComment>(modelBuilder);
        ConfigureTenantEntity<Tag>(modelBuilder);
        ConfigureTenantEntity<Category>(modelBuilder);
        ConfigureTenantEntity<ContentTag>(modelBuilder);
        ConfigureTenantEntity<ContentCategory>(modelBuilder);

        _ = modelBuilder.Entity<ContentItem>()
            .HasIndex(content => content.Slug)
            .IsUnique();

        _ = modelBuilder.Entity<Tag>()
            .HasIndex(tag => tag.Slug)
            .IsUnique();

        _ = modelBuilder.Entity<Category>()
            .HasIndex(category => category.Slug)
            .IsUnique();

        _ = modelBuilder.Entity<ContentComment>()
            .HasOne(comment => comment.Content)
            .WithMany(content => content.Comments)
            .HasForeignKey(comment => comment.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ContentComment>()
            .HasOne(comment => comment.ParentComment)
            .WithMany(parent => parent.Replies)
            .HasForeignKey(comment => comment.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Category>()
            .HasOne(category => category.ParentCategory)
            .WithMany(parent => parent.Subcategories)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ContentTag>()
            .HasIndex(contentTag => new { contentTag.ContentId, contentTag.TagId })
            .IsUnique();

        _ = modelBuilder.Entity<ContentTag>()
            .HasOne(contentTag => contentTag.Content)
            .WithMany(content => content.Tags)
            .HasForeignKey(contentTag => contentTag.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ContentTag>()
            .HasOne(contentTag => contentTag.Tag)
            .WithMany(tag => tag.ContentTags)
            .HasForeignKey(contentTag => contentTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ContentCategory>()
            .HasIndex(contentCategory => new { contentCategory.ContentId, contentCategory.CategoryId })
            .IsUnique();

        _ = modelBuilder.Entity<ContentCategory>()
            .HasOne(contentCategory => contentCategory.Content)
            .WithMany(content => content.Categories)
            .HasForeignKey(contentCategory => contentCategory.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ContentCategory>()
            .HasOne(contentCategory => contentCategory.Category)
            .WithMany(category => category.ContentCategories)
            .HasForeignKey(contentCategory => contentCategory.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }
    }

    private void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(entity =>
            EF.Property<DateTimeOffset?>(entity, "DeletedAt") == null);

        _ = entityBuilder.IsMultiTenant();
    }
}
