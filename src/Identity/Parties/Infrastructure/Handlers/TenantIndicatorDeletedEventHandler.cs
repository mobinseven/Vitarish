using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Handlers;

/// <summary>
/// Projects TenantIndicator deletion to AuthenticationClient.
/// </summary>
public sealed class TenantIndicatorDeletedEventHandler(
    AuthenticationClientDbContext authDb,
    ILogger<TenantIndicatorDeletedEventHandler> logger)
    : INotificationHandler<TenantIndicatorDeletedEvent>
{
    public async Task Handle(TenantIndicatorDeletedEvent notification, CancellationToken ct)
    {
        try
        {
            await DeleteAuthClient(notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project TenantIndicatorDeleted event for TenantId={TenantId}, IndicatorId={IndicatorId}",
                notification.TenantId, notification.IndicatorId);
            throw;
        }
    }

    private async Task DeleteAuthClient(TenantIndicatorDeletedEvent ev, CancellationToken ct)
    {
        string clientId = AuthClientIdGenerator.Generate(ev.TenantId, ev.Method, ev.Value);
        AuthenticationClient? client = await authDb.AuthenticationClients.FindAsync([clientId], ct);

        if (client is null)
        {
            logger.LogWarning("AuthenticationClient not found for deleted indicator: ClientId={ClientId}", clientId);
            return;
        }

        _ = authDb.AuthenticationClients.Remove(client);
        _ = await authDb.SaveChangesAsync(ct);

        logger.LogInformation("Deleted AuthenticationClient: ClientId={ClientId}, TenantId={TenantId}",
            clientId, ev.TenantId);
    }
}
