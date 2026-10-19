using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class TenantOriginTrustTests
{
    private const string BaseDomain = "clinic.example.test";
    private const string TenantHost = "rezaei.clinic.example.test";

    [Theory]
    [InlineData("https://rezaei.clinic.example.test")]
    [InlineData("https://rezaei.clinic.example.test:8443")]
    public async Task Trusts_https_origin_with_active_subdomain_indicator_with_or_without_port(string origin)
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, TenantHost, IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(BaseDomain));

        Assert.True(await trust.IsTenantOriginAsync(new Uri(origin)));
    }

    [Fact]
    public async Task Rejects_plain_http_origin_for_non_localhost_base_domain()
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, TenantHost, IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(BaseDomain));

        Assert.False(await trust.IsTenantOriginAsync(new Uri("http://rezaei.clinic.example.test")));
    }

    [Fact]
    public async Task Trusts_plain_http_origin_under_localhost_base_domain()
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, "rezaei.localhost", IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor("localhost"));

        Assert.True(await trust.IsTenantOriginAsync(new Uri("http://rezaei.localhost:3005")));
    }

    [Theory]
    [InlineData(IndicatorMethod.Subdomain, IndicatorStatus.Disabled)]
    [InlineData(IndicatorMethod.Subdomain, IndicatorStatus.PendingVerification)]
    [InlineData(IndicatorMethod.Hostname, IndicatorStatus.Active)]
    public async Task Rejects_non_active_or_non_subdomain_indicators(IndicatorMethod method, IndicatorStatus status)
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, TenantHost, method, status);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(BaseDomain));

        Assert.False(await trust.IsTenantOriginAsync(new Uri("https://rezaei.clinic.example.test")));
    }

    [Theory]
    [InlineData("https://rezaei.other.test")]
    [InlineData("https://clinic.example.test")]
    [InlineData("https://a.rezaei.clinic.example.test")]
    [InlineData("https://admin.clinic.example.test")]
    public async Task Rejects_hosts_outside_single_unreserved_subdomain_shape(string origin)
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, TenantHost, IndicatorMethod.Subdomain, IndicatorStatus.Active);
        SeedTenantWithIndicator(db, "admin.clinic.example.test", IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(BaseDomain));

        Assert.False(await trust.IsTenantOriginAsync(new Uri(origin)));
    }

    [Fact]
    public async Task Rejects_when_no_base_domain_is_configured()
    {
        await using PartiesDbContext db = CreateContext();
        SeedTenantWithIndicator(db, TenantHost, IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(null));

        Assert.False(await trust.IsTenantOriginAsync(new Uri("https://rezaei.clinic.example.test")));
    }

    [Fact]
    public async Task Caches_trust_decision_until_hostname_change_evicts_cache()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using PartiesDbContext db = CreateContext(dbName);
        SeedTenantWithIndicator(db, TenantHost, IndicatorMethod.Subdomain, IndicatorStatus.Active);
        await db.SaveChangesAsync();
        using MemoryCache cache = new(new MemoryCacheOptions());
        TenantOriginTrust trust = new(db, cache, OptionsFor(BaseDomain));
        Uri origin = new("https://rezaei.clinic.example.test");

        Assert.True(await trust.IsTenantOriginAsync(origin));

        await using (PartiesDbContext secondDb = CreateContext(dbName))
        {
            TenantIndicator indicator = await secondDb.TenantIndicators.SingleAsync();
            indicator.Status = IndicatorStatus.Disabled;
            await secondDb.SaveChangesAsync();
        }

        Assert.True(await trust.IsTenantOriginAsync(origin));

        await new TenantHostCacheEviction(cache).Handle(new TenantHostnamesChangedEvent([TenantHost]), default);

        Assert.False(await trust.IsTenantOriginAsync(origin));
    }

    private static void SeedTenantWithIndicator(
        PartiesDbContext db,
        string host,
        IndicatorMethod method,
        IndicatorStatus status)
    {
        Tenant tenant = new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = host, AppId = host };
        db.Tenants.Add(tenant);
        db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = method,
            Value = host,
            Status = status,
            IsPrimary = true
        });
    }

    private static IOptions<TenantAddressOptions> OptionsFor(string? baseDomain) =>
        Options.Create(new TenantAddressOptions { TenantBaseDomain = baseDomain });

    private static PartiesDbContext CreateContext(string? dbName = null) => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString("N")).Options,
        Mock.Of<MediatR.IPublisher>(),
        Array.Empty<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
}
