using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class TenantResolverTests
{
    [Fact]
    public async Task Resolves_two_hosts_to_their_own_tenants_without_cache_crossover()
    {
        await using PartiesDbContext db = CreateContext();
        Tenant tenantA = AddTenant(db, "tenant-a.example.test", "Tenant A");
        Tenant tenantB = AddTenant(db, "tenant-b.example.test", "Tenant B");
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantResolver resolver = new(db, cache);

        Assert.Equal(tenantA.Id, (await resolver.ResolveAsync(Context("tenant-a.example.test")))!.Id);
        Assert.Equal(tenantB.Id, (await resolver.ResolveAsync(Context("tenant-b.example.test")))!.Id);
    }

    [Fact]
    public async Task Ignores_a_forged_tenant_header_when_origin_disagrees()
    {
        await using PartiesDbContext db = CreateContext();
        Tenant tenantA = AddTenant(db, "tenant-a.example.test", "Tenant A");
        Tenant tenantB = AddTenant(db, "tenant-b.example.test", "Tenant B");
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantResolver resolver = new(db, cache);
        DefaultHttpContext context = Context("tenant-a.example.test");
        context.Request.Headers[TenantResolver.TenantHostHeaderName] = "tenant-b.example.test";
        context.Request.Headers.Origin = "https://tenant-a.example.test";

        Assert.Equal(tenantA.Id, (await resolver.ResolveAsync(context))!.Id);
        Assert.NotEqual(tenantB.Id, (await resolver.ResolveAsync(context))!.Id);
    }

    [Fact]
    public async Task Uses_the_trusted_forwarded_host_after_forwarded_headers_are_applied()
    {
        await using PartiesDbContext db = CreateContext();
        Tenant tenantB = AddTenant(db, "tenant-b.example.test", "Tenant B");
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantResolver resolver = new(db, cache);
        DefaultHttpContext context = new();
        context.Request.Host = new HostString("api.example.test");
        context.Request.Headers[TenantResolver.TenantHostHeaderName] = new StringValues("tenant-b.example.test");

        Assert.Equal(tenantB.Id, (await resolver.ResolveAsync(context))!.Id);
    }

    private static Tenant AddTenant(PartiesDbContext db, string host, string name)
    {
        Tenant tenant = new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = name, AppId = name };
        db.Tenants.Add(tenant);
        db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Method = IndicatorMethod.Hostname,
            Value = host, Status = IndicatorStatus.Active, IsPrimary = true
        });
        return tenant;
    }

    private static DefaultHttpContext Context(string host)
    {
        DefaultHttpContext context = new();
        context.Request.Host = new HostString(host);
        context.Request.Headers[TenantResolver.TenantHostHeaderName] = host;
        return context;
    }

    private static PartiesDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
        Mock.Of<MediatR.IPublisher>(), Array.Empty<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
}
