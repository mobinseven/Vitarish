
using MediatR;
using Microsoft.Extensions.Logging;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Extensions;

namespace Vorn.Aaas.Parties.Application.EventHandlers;
/// <summary>
/// Projects TenantIndicator deletion to AuthenticationClient.
/// </summary>
internal sealed class TenantIndicatorDeletedEventHandler(
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
