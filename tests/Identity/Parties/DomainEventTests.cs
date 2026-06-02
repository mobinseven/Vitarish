
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;

namespace Parties.Tests;

public class DomainEventTests
{
    [Fact]
    public void OwnerCreatedEvent_StoresOwnerId()
    {
        Guid ownerId = Guid.NewGuid();
        OwnerCreatedEvent notification = new(ownerId);

        Assert.Equal(ownerId, notification.OwnerId);
    }

    [Fact]
    public void TenantCreatedEvent_StoresTenantAndOwnerIds()
    {
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();

        TenantCreatedEvent notification = new(tenantId, ownerId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(ownerId, notification.OwnerId);
    }

    [Fact]
    public void TenantIndicatorCreatedEvent_StoresIds()
    {
        Guid tenantId = Guid.NewGuid();
        Guid indicatorId = Guid.NewGuid();

        TenantIndicatorCreatedEvent notification = new(tenantId, indicatorId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(indicatorId, notification.IndicatorId);
    }

    [Fact]
    public void TenantIndicatorUpdatedEvent_StoresIds()
    {
        Guid tenantId = Guid.NewGuid();
        Guid indicatorId = Guid.NewGuid();

        TenantIndicatorUpdatedEvent notification = new(tenantId, indicatorId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(indicatorId, notification.IndicatorId);
    }

    [Fact]
    public void TenantIndicatorDeletedEvent_StoresPayload()
    {
        Guid tenantId = Guid.NewGuid();
        Guid indicatorId = Guid.NewGuid();

        TenantIndicatorDeletedEvent notification = new(
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
