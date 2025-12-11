using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Handlers;

/// <summary>
/// Projects TenantIndicator updates to AuthenticationClient.
/// </summary>
public sealed class TenantIndicatorUpdatedEventHandler(
    PartiesDbContext partiesDb,
    AuthenticationClientDbContext authDb,
    ILogger<TenantIndicatorUpdatedEventHandler> logger)
    : INotificationHandler<TenantIndicatorUpdatedEvent>
{
    public async Task Handle(TenantIndicatorUpdatedEvent notification, CancellationToken ct)
    {
        try
        {
            await UpsertAuthClient(notification.TenantId, notification.IndicatorId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project TenantIndicatorUpdated event for TenantId={TenantId}, IndicatorId={IndicatorId}",
                notification.TenantId, notification.IndicatorId);
            throw;
        }
    }

    private async Task UpsertAuthClient(Guid tenantId, Guid indicatorId, CancellationToken ct)
    {
        TenantIndicator? indicator = await partiesDb.TenantIndicators
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == indicatorId && i.TenantId == tenantId, ct);

        if (indicator is null)
        {
            logger.LogWarning("TenantIndicator not found: TenantId={TenantId}, IndicatorId={IndicatorId}",
                tenantId, indicatorId);
            return;
        }

        string clientId = AuthClientMappingExtensions.GenerateClientId(indicator.TenantId, indicator.Method, indicator.Value);
        AuthenticationClient? client = await authDb.AuthenticationClients.FindAsync([clientId], ct);

        if (client is null)
        {
            client = AuthClientMappingExtensions.CreateClient(indicator, indicator.Tenant);
            _ = authDb.AuthenticationClients.Add(client);
        }
        else
        {
            AuthClientMappingExtensions.MapToClient(indicator, indicator.Tenant, client);
            _ = authDb.AuthenticationClients.Update(client);
        }

        _ = await authDb.SaveChangesAsync(ct);

        logger.LogInformation("Projected TenantIndicator to AuthenticationClient: ClientId={ClientId}, TenantId={TenantId}",
            clientId, tenantId);
    }
}
