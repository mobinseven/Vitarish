
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Vorn.Aaas.Parties.Infrastructure.Extensions;

namespace Vorn.Aaas.Parties.Application.EventHandlers;
/// <summary>
/// Projects TenantIndicator creation to AuthenticationClient.
/// </summary>
internal sealed class TenantIndicatorCreatedEventHandler(
    PartiesDbContext partiesDb,
    AuthenticationClientDbContext authDb,
    ILogger<TenantIndicatorCreatedEventHandler> logger)
    : INotificationHandler<TenantIndicatorCreatedEvent>
{
    public async Task Handle(TenantIndicatorCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            await UpsertAuthClient(notification.TenantId, notification.IndicatorId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project TenantIndicatorCreated event for TenantId={TenantId}, IndicatorId={IndicatorId}",
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
