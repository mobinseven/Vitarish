# Vitarish.Entities.Generators

`Vitarish.Entities.Generators` is a Roslyn source generator that emits dependency injection helpers for every aggregate described in your assembly. By annotating your domain project once, the generator produces extension methods that register MediatR handlers, caches, and interception services without hand-writing boilerplate.

## Add the analyzer reference

Include the generator as an analyzer reference in the project that defines your entities and descriptors:

```bash
dotnet add package Vitarish.Entities.Generators
```

> 📌 The package is a source generator, so it only needs to be referenced by the project that produces the entities. Downstream consumers (API, server, client) will use the generated extensions at compile time.

## Opt-in per assembly

Decorate your assembly with `[GenerateEntitiesRegistration]` to activate generation.

```csharp
using Vitarish.Entities.Generators;

[assembly: GenerateEntitiesRegistration]
```

The generator scans the compilation for descriptor types decorated with `[EntityDescriptorFor(typeof(TEntity))]` where `TEntity` inherits from `Vitarish.Entities.Entity`.

## Describe descriptors

Use the descriptor attribute on your descriptor type so the generator can pair it with the entity:

```csharp
[EntityDescriptorFor(typeof(Document))]
public sealed record DocumentDescriptor : EntityDescriptor
{
    public string? SearchText { get; init; }
}
```

You can repeat the attribute for every aggregate in the assembly. Abstract descriptors/entities are ignored automatically.

## What gets generated

When descriptors are found, the generator emits an extension class named `EntityManagementRegistrationOf<YourAssemblyName>` (dots removed). The method wires common services:

```csharp
/// <summary>
/// Auto-generated registration extensions for DocumentsDomain entities.
/// </summary>
public static class EntityManagementRegistrationOfDocumentsDomain
{
    /// <summary>
    /// Registers all entity handlers, repositories, and core services for DocumentsDomain.
    /// </summary>
    /// <remarks>
    /// This method registers:
    /// - MediatR command and query handlers for all entities
    /// - Memory cache implementations (IEntityCache)
    /// - Cache invalidation services
    /// - Entity interceptor for audit trails
    /// - User and time services
    /// 
    /// Optional enhancements (call separately):
    /// - services.AddEntityValidation() - FluentValidation pipeline behavior
    /// - services.AddDistributedEntityCache<T>() - Redis/distributed caching
    /// - services.AddCachedEntityRepository<T, TDescriptor>() - Cache-aside pattern
    /// - services.AddHealthChecks().AddEntityCacheHealthCheck<T>() - Health monitoring
    /// </remarks>
    public static void AddDocumentsDomainEntityHandlers(this IServiceCollection services)
    {
        // Core infrastructure
        services.AddMemoryCache();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EntityManagementRegistrationOfDocumentsDomain).Assembly));
        services.AddSingleton<IEntityCacheInvalidationService, EntityCacheInvalidationService>();
        services.AddScoped<EntityInterceptor>();
        services.AddScoped<IEntityUserService, EntityUserService>();
        services.AddScoped<IEntityTimeService, EntityTimeService>();

        // Document handlers and cache
        services.AddTransient<IRequestHandler<AddEntityCommand<Document, DocumentDescriptor>, Document?>, AddEntityHandler<Document, DocumentDescriptor>>();
        // ...additional handlers per aggregate...
    }
}
```

The registrations cover every command/query handler (`Add`, `Update`, `Remove`, `Get`, `Count`) along with the cache implementations needed by the core library.

## Consuming the generated code

Call the extension method from your host or composition root:

```csharp
// Basic registration (required)
builder.Services.AddDocumentsDomainEntityHandlers();

// Optional enhancements for production
builder.Services.AddEntityValidation(); // FluentValidation pipeline

// For read-heavy workloads with Redis
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = "redis:6379");
builder.Services.AddDistributedEntityCache<Document>(); // Redis caching
builder.Services.AddCachedEntityRepository<Document, DocumentDescriptor>(); // Cache-aside pattern

// Health checks
builder.Services.AddHealthChecks().AddEntityCacheHealthCheck<Document>();
```

Because the extension lives in the same assembly as your entities, it travels with the project wherever it is referenced. IDEs expose the generated file in the *Analyzers → Documents* node so you can inspect the output.

### Optional Enhancements

The generator provides core registrations. Additional production-grade features are available via extension methods:

- **AddEntityValidation()** - Adds FluentValidation pipeline behavior for input validation
- **AddDistributedEntityCache&lt;T&gt;()** - Replaces memory cache with Redis/IDistributedCache (requires AddStackExchangeRedisCache or similar)
- **AddCachedEntityRepository&lt;T, TDescriptor&gt;()** - Implements cache-aside pattern at repository level (4-10x read performance improvement)
- **AddEntityCacheHealthCheck&lt;T&gt;()** - Adds ASP.NET Core health check for cache monitoring

See the main documentation for detailed usage of these enhancements.

## Tips

- Commit the attributes alongside your DTO/descriptor definitions so the generator immediately tracks new aggregates.
- If you need bespoke registrations (e.g., custom caching), you can still call the generated method and then override specific services.
- Combine with `dotnet format`/`dotnet build` in CI to catch analyzer errors early; the generator surfaces diagnostics if descriptor/entity types cannot be resolved.
