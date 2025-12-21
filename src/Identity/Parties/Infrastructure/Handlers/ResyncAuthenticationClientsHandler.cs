using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class ResyncAuthenticationClientsHandler(
    PartiesDbContext partiesDb,
    AuthenticationClientDbContext authDb,
    ILogger<ResyncAuthenticationClientsHandler> logger)
    : IRequestHandler<ResyncAuthenticationClients, ResyncAuthenticationClientsResult>
{
    public async Task<ResyncAuthenticationClientsResult> Handle(ResyncAuthenticationClients request, CancellationToken ct)
    {
        int created = 0;
        int updated = 0;
        int deleted = 0;
        int unchanged = 0;

        IQueryable<TenantIndicator> query = partiesDb.TenantIndicators.Include(i => i.Tenant).AsQueryable();
        if (request.TenantId.HasValue)
        {
            query = query.Where(i => i.TenantId == request.TenantId.Value);
        }

        List<TenantIndicator> indicators = await query.ToListAsync(ct);

        HashSet<string> indicatorIds = [.. indicators.Select(i => AuthClientMappingExtensions.GenerateClientId(i.TenantId, i.Method, i.Value))];

        Dictionary<string, AuthenticationClient> existingClients = await authDb.AuthenticationClients
            .Where(c => c.Id.StartsWith("ac:"))
            .ToDictionaryAsync(c => c.Id, ct);

        foreach (TenantIndicator? indicator in indicators)
        {
            string clientId = AuthClientMappingExtensions.GenerateClientId(indicator.TenantId, indicator.Method, indicator.Value);

            if (!existingClients.TryGetValue(clientId, out AuthenticationClient? existingClient))
            {
                created++;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("{Action} AuthenticationClient: {ClientId} for TenantId={TenantId}",
                        request.DryRun ? "Would create" : "Creating", clientId, indicator.TenantId);
                }

                if (!request.DryRun)
                {
                    AuthenticationClient client = AuthClientMappingExtensions.CreateClient(indicator, indicator.Tenant);
                    _ = authDb.AuthenticationClients.Add(client);
                }
            }
            else
            {
                AuthenticationClient expectedClient = AuthClientMappingExtensions.CreateClient(indicator, indicator.Tenant);

                if (existingClient.Url != expectedClient.Url ||
                    existingClient.Title != expectedClient.Title ||
                    existingClient.Description != expectedClient.Description ||
                    existingClient.LogoUrl != expectedClient.LogoUrl ||
                    existingClient.AutoAccess != expectedClient.AutoAccess)
                {
                    updated++;
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("{Action} AuthenticationClient: {ClientId} for TenantId={TenantId}",
                            request.DryRun ? "Would update" : "Updating", clientId, indicator.TenantId);
                    }

                    if (!request.DryRun)
                    {
                        AuthClientMappingExtensions.MapToClient(indicator, indicator.Tenant, existingClient);
                        _ = authDb.AuthenticationClients.Update(existingClient);
                    }
                }
                else
                {
                    unchanged++;
                }

                _ = existingClients.Remove(clientId);
            }
        }

        foreach (AuthenticationClient? orphan in existingClients.Values.Where(c => c.Id.StartsWith("ac:")))
        {
            deleted++;
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("{Action} orphaned AuthenticationClient: {ClientId}",
                    request.DryRun ? "Would delete" : "Deleting", orphan.Id);
            }

            if (!request.DryRun)
            {
                _ = authDb.AuthenticationClients.Remove(orphan);
            }
        }

        if (!request.DryRun)
        {
            _ = await authDb.SaveChangesAsync(ct);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Resync complete (DryRun={DryRun}): Created={Created}, Updated={Updated}, Deleted={Deleted}, Unchanged={Unchanged}",
                request.DryRun, created, updated, deleted, unchanged);
        }

        return new ResyncAuthenticationClientsResult(created, updated, deleted, unchanged);
    }
}
