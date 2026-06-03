using Vitarish.Entities.Server.Repositories.Operations;

namespace Vitarish.Entities.Server.Extensions;
/// <summary>
/// Extension methods for configuring Vitarish.Entities services with enhanced features.
/// </summary>
public static class ServiceCollectionExtensions
{

    /// <summary>
    /// Registers a transient non-tenant DbContext and its factory using the platform creation path.
    /// </summary>
    public static IServiceCollection AddTransientDbContext<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        _ = services.AddDbContext<TDbContext>(configureDbContext, ServiceLifetime.Transient);
        services.Replace(ServiceDescriptor.Transient<IDbContextFactory<TDbContext>, TransientDbContextFactory<TDbContext>>());
        services.Replace(ServiceDescriptor.Transient<TDbContext>(provider => provider.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext()));

        return services;
    }

    /// <summary>
    /// Registers a transient multi-tenant DbContext and its factory using the platform creation path.
    /// </summary>
    public static IServiceCollection AddTransientMultiTenantDbContext<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        where TDbContext : DbContext, Finbuckle.MultiTenant.EntityFrameworkCore.IMultiTenantDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        _ = services.AddDbContext<TDbContext>(configureDbContext, ServiceLifetime.Transient);
        services.Replace(ServiceDescriptor.Transient<IDbContextFactory<TDbContext>, TransientMultiTenantDbContextFactory<TDbContext>>());
        services.Replace(ServiceDescriptor.Transient<TDbContext>(provider => provider.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext()));

        return services;
    }

    /// <summary>
    /// Adds FluentValidation pipeline behavior to MediatR for request validation.
    /// </summary>
    public static IServiceCollection AddEntityValidation(this IServiceCollection services)
    {
        _ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Adds distributed cache implementation for entities using IDistributedCache (e.g., Redis).
    /// Requires IDistributedCache to be registered (e.g., via AddStackExchangeRedisCache).
    /// </summary>
    public static IServiceCollection AddDistributedEntityCache<TEntity>(
        this IServiceCollection services,
        Action<DistributedCacheEntryOptions>? configureOptions = null)
        where TEntity : Entity
    {
        _ = services.AddSingleton<IEntityCache<TEntity>>(provider =>
        {
            IDistributedCache cache = provider.GetRequiredService<IDistributedCache>();
            DistributedCacheEntryOptions options = new()
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            configureOptions?.Invoke(options);
            return new EntityDistributedCache<TEntity>(cache, options);
        });
        return services;
    }

    /// <summary>
    /// Adds health checks for entity cache operations.
    /// Requires TEntity to have a parameterless constructor for health check testing.
    /// </summary>
    public static IHealthChecksBuilder AddEntityCacheHealthCheck<TEntity>(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        string[]? tags = null)
        where TEntity : Entity, new() => builder.AddCheck<EntityCacheHealthCheck<TEntity>>(
            name ?? $"entity_cache_{typeof(TEntity).Name}",
            failureStatus,
            tags ?? Array.Empty<string>());

    /// <summary>
    /// Adds cached repository decorator for entity operations.
    /// Implements cache-aside pattern: reads check cache first, writes go through to database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>IMPORTANT - Registration Order:</b> This method should be called AFTER registering the base repository and cache.
    /// The decorator pattern requires the inner service to be registered first. If the base repository is not registered,
    /// this method will silently skip decoration (using TryDecorate pattern), and the application will use the base repository
    /// directly without caching.
    /// </para>
    /// <para>
    /// <b>Recommended Registration Order:</b>
    /// </para>
    /// <code>
    /// // 1. Register base repository first
    /// services.AddScoped&lt;IEntityRepository&lt;MyEntity, MyDescriptor&gt;&gt;, MyRepository&gt;();
    /// 
    /// // 2. Register cache
    /// services.AddSingleton&lt;IEntityCache&lt;MyEntity&gt;&gt;, EntityMemoryCache&lt;MyEntity&gt;&gt;();
    /// 
    /// // 3. Finally, add the cached repository decorator
    /// services.AddCachedEntityRepository&lt;MyEntity, MyDescriptor&gt;();
    /// </code>
    /// <para>
    /// Works with EntityInterceptor for automatic cache invalidation on updates/deletes.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCachedEntityRepository<TEntity, TDescriptor>(
        this IServiceCollection services)
        where TEntity : Entity
        where TDescriptor : EntityDescriptor
    {
        // Use TryDecorate to gracefully handle missing registrations
        // If base repository is not registered, decoration is skipped without error
        bool decorated = services.TryDecorate<IEntityRepository<TEntity, TDescriptor>>((inner, provider) =>
        {
            IEntityCache<TEntity> cache = provider.GetRequiredService<IEntityCache<TEntity>>();
            return new CachedEntityRepository<TEntity, TDescriptor>(inner, cache);
        });

        // Note: If decorated is false, base repository wasn't registered
        // Application will still work but without caching layer

        return services;
    }

    /// <summary>
    /// Registers <see cref="IEntityContextFactory{TDbContext}"/> so that repositories whose
    /// <c>CreateOperationScope</c> method is overridden to use the factory will receive a fresh
    /// <typeparamref name="TDbContext"/> per repository call instead of reusing the
    /// constructor-injected context.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="IDbContextFactory{TContext}"/> to be registered in the container.
    /// Call <c>AddDbContextFactory&lt;TDbContext&gt;</c> (or use the overload of
    /// <c>AddDbContext</c> that registers the factory) before calling this method.
    /// </remarks>
    public static IServiceCollection AddEntityContextFactory<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        _ = services.AddSingleton<IEntityContextFactory<TDbContext>, TransientEntityContextFactory<TDbContext>>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IEntityUnitOfWorkFactory{TDbContext}"/> for coordinating multiple
    /// repository operations within a single database transaction.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="IDbContextFactory{TContext}"/> to be registered in the container.
    /// Call <c>AddDbContextFactory&lt;TDbContext&gt;</c> (or use the overload of
    /// <c>AddDbContext</c> that registers the factory) before calling this method.
    /// </remarks>
    public static IServiceCollection AddEntityUnitOfWorkFactory<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        _ = services.AddTransient<IEntityUnitOfWorkFactory<TDbContext>, EntityUnitOfWorkFactory<TDbContext>>();
        return services;
    }
}
