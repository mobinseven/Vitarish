using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class TenantHostnameChangeEventTests
{
    [Fact]
    public async Task Adding_an_indicator_publishes_normalized_hostname()
    {
        await using PartiesDbContext db = CreateContext(out List<TenantHostnamesChangedEvent> published);
        Tenant tenant = CreateTenant(db);
        db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "Rezaei.Clinic.Example.Test.",
            Status = IndicatorStatus.Active
        });

        await db.SaveChangesAsync();

        TenantHostnamesChangedEvent ev = Assert.Single(published);
        Assert.Equal(["rezaei.clinic.example.test"], ev.Hostnames);
    }

    [Fact]
    public async Task Changing_an_indicator_value_publishes_both_old_and_new_hostnames()
    {
        await using PartiesDbContext db = CreateContext(out List<TenantHostnamesChangedEvent> published);
        Tenant tenant = CreateTenant(db);
        TenantIndicator indicator = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "old.clinic.example.test",
            Status = IndicatorStatus.Active
        };
        db.TenantIndicators.Add(indicator);
        await db.SaveChangesAsync();
        published.Clear();

        indicator.Value = "new.clinic.example.test";
        await db.SaveChangesAsync();

        TenantHostnamesChangedEvent ev = Assert.Single(published);
        Assert.Equal(2, ev.Hostnames.Count);
        Assert.Contains("old.clinic.example.test", ev.Hostnames);
        Assert.Contains("new.clinic.example.test", ev.Hostnames);
    }

    [Fact]
    public async Task Removing_an_indicator_publishes_its_hostname()
    {
        await using PartiesDbContext db = CreateContext(out List<TenantHostnamesChangedEvent> published);
        Tenant tenant = CreateTenant(db);
        TenantIndicator indicator = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "rezaei.clinic.example.test",
            Status = IndicatorStatus.Active
        };
        db.TenantIndicators.Add(indicator);
        await db.SaveChangesAsync();
        published.Clear();

        db.TenantIndicators.Remove(indicator);
        await db.SaveChangesAsync();

        TenantHostnamesChangedEvent ev = Assert.Single(published);
        Assert.Equal(["rezaei.clinic.example.test"], ev.Hostnames);
    }

    [Fact]
    public async Task Saving_changes_without_indicator_modifications_publishes_no_hostname_change_event()
    {
        await using PartiesDbContext db = CreateContext(out List<TenantHostnamesChangedEvent> published);
        Tenant tenant = CreateTenant(db);
        await db.SaveChangesAsync();
        published.Clear();

        tenant.Name = "Renamed Tenant";
        await db.SaveChangesAsync();

        Assert.Empty(published);
    }

    [Fact]
    public async Task Eviction_handler_removes_resolver_and_origin_trust_keys_for_changed_host_only()
    {
        const string changedHost = "rezaei.clinic.example.test";
        const string otherHost = "other.clinic.example.test";
        using MemoryCache cache = new(new MemoryCacheOptions());
        cache.Set(TenantHostCacheKeys.Resolver(changedHost), "resolver-changed");
        cache.Set(TenantHostCacheKeys.OriginTrust(changedHost), true);
        cache.Set(TenantHostCacheKeys.Resolver(otherHost), "resolver-other");
        cache.Set(TenantHostCacheKeys.OriginTrust(otherHost), true);

        await new TenantHostCacheEviction(cache).Handle(new TenantHostnamesChangedEvent([changedHost]), default);

        Assert.False(cache.TryGetValue(TenantHostCacheKeys.Resolver(changedHost), out _));
        Assert.False(cache.TryGetValue(TenantHostCacheKeys.OriginTrust(changedHost), out _));
        Assert.True(cache.TryGetValue(TenantHostCacheKeys.Resolver(otherHost), out _));
        Assert.True(cache.TryGetValue(TenantHostCacheKeys.OriginTrust(otherHost), out _));
    }

    private static Tenant CreateTenant(PartiesDbContext db)
    {
        Tenant tenant = new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Tenant", AppId = "app" };
        db.Tenants.Add(tenant);
        return tenant;
    }

    private static PartiesDbContext CreateContext(out List<TenantHostnamesChangedEvent> published)
    {
        List<TenantHostnamesChangedEvent> events = [];
        Mock<IPublisher> publisher = new();
        _ = publisher
            .Setup(p => p.Publish(It.IsAny<TenantHostnamesChangedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TenantHostnamesChangedEvent, CancellationToken>((ev, _) => events.Add(ev))
            .Returns(Task.CompletedTask);
        published = events;

        return new PartiesDbContext(
            new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
            publisher.Object,
            Array.Empty<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
    }
}
