using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Commerce.Infrastructure.Data;

public class CommerceDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
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

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    public CommerceDbContext(
        DbContextOptions<CommerceDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;
        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
            .AddInterceptors(_interceptors);

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

    private static readonly JsonSerializerOptions _jsonOptions = new();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureTenantEntity<Store>(modelBuilder);
        ConfigureTenantEntity<Product>(modelBuilder);
        ConfigureTenantEntity<ProductVariant>(modelBuilder);
        ConfigureTenantEntity<Category>(modelBuilder);
        ConfigureTenantEntity<Order>(modelBuilder);
        ConfigureTenantEntity<OrderItem>(modelBuilder);
        ConfigureTenantEntity<Cart>(modelBuilder);
        ConfigureTenantEntity<CartItem>(modelBuilder);
        ConfigureTenantEntity<CustomerProfile>(modelBuilder);

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }

        _ = modelBuilder.Entity<StoreStats>(eb =>
        {
            _ = eb.ToView("vw_StoreStats");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Store>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, _jsonOptions))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
            entity.Ignore(s => s.Stats);
        });

        _ = modelBuilder.Entity<OrderStats>(eb =>
        {
            _ = eb.ToView("vw_OrderStats");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Order>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, _jsonOptions))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
            entity.Ignore(o => o.Stats);
        });

        _ = modelBuilder.Entity<Product>(entity =>
        {
            _ = entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, _jsonOptions))
                .HasDefaultValueSql("'{}'")
                .HasColumnType("nvarchar(max)");
        });
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();
        _ = entityBuilder.HasQueryFilter(e =>
            EF.Property<DateTimeOffset?>(e, "DeletedAt") == null);
        _ = entityBuilder.IsMultiTenant();
    }
}
