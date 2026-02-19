namespace Parties.Tests;

using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Domain.Enums;

public class DomainEventTests
{
    [Fact]
    public void OwnerCreatedEvent_StoresOwnerId()
    {
        var ownerId = Guid.NewGuid();
        var notification = new OwnerCreatedEvent(ownerId);

        Assert.Equal(ownerId, notification.OwnerId);
    }

    [Fact]
    public void TenantCreatedEvent_StoresTenantAndOwnerIds()
    {
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var notification = new TenantCreatedEvent(tenantId, ownerId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(ownerId, notification.OwnerId);
    }

    [Fact]
    public void TenantIndicatorCreatedEvent_StoresIds()
    {
        var tenantId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        var notification = new TenantIndicatorCreatedEvent(tenantId, indicatorId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(indicatorId, notification.IndicatorId);
    }

    [Fact]
    public void TenantIndicatorUpdatedEvent_StoresIds()
    {
        var tenantId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        var notification = new TenantIndicatorUpdatedEvent(tenantId, indicatorId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(indicatorId, notification.IndicatorId);
    }

    [Fact]
    public void TenantIndicatorDeletedEvent_StoresPayload()
    {
        var tenantId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        var notification = new TenantIndicatorDeletedEvent(
            tenantId,
            indicatorId,
            IndicatorMethod.Subdomain,
            "tenant");

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(indicatorId, notification.IndicatorId);
        Assert.Equal(IndicatorMethod.Subdomain, notification.Method);
        Assert.Equal("tenant", notification.Value);
    }
}
