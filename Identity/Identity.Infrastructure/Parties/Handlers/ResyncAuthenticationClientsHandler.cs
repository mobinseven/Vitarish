using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.Parties.Data;
using Vitarish.Identity.Infrastructure.Parties.Extensions;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class ResyncAuthenticationClientsHandler(
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

        IQueryable<TenantIndicator> query = partiesDb.TenantIndicators.Include(i => i.Tenant);
        if (request.TenantId.HasValue)
        {
            query = query.Where(i => i.TenantId == request.TenantId.Value);
        }

        List<TenantIndicator> indicators = await query.ToListAsync(ct);

        Dictionary<string, AuthenticationClient> existingClients = await authDb.AuthenticationClients
            .Where(c => c.Id.StartsWith("ac:"))
            .ToDictionaryAsync(c => c.Id, ct);

        foreach (TenantIndicator indicator in indicators)
        {
            string clientId = AuthClientMappingExtensions.GenerateClientId(indicator.TenantId, indicator.Method, indicator.Value);
            if (!existingClients.TryGetValue(clientId, out AuthenticationClient? existingClient))
            {
                created++;
                logger.LogInformation("{Action} AuthenticationClient: {ClientId} for TenantId={TenantId}",
                    request.DryRun ? "Would create" : "Creating", clientId, indicator.TenantId);

                if (!request.DryRun && indicator.Tenant is not null)
                {
                    AuthenticationClient client = AuthClientMappingExtensions.CreateClient(indicator, indicator.Tenant);
                    _ = authDb.AuthenticationClients.Add(client);
                }
            }
            else
            {
                AuthenticationClient? expectedClient = indicator.Tenant is null
                    ? null
                    : AuthClientMappingExtensions.CreateClient(indicator, indicator.Tenant);

                if (expectedClient is not null &&
                    (existingClient.Url != expectedClient.Url ||
                     existingClient.Title != expectedClient.Title ||
                     existingClient.Description != expectedClient.Description ||
                     existingClient.LogoUrl != expectedClient.LogoUrl ||
                     existingClient.AutoAccess != expectedClient.AutoAccess))
                {
                    updated++;
                    logger.LogInformation("{Action} AuthenticationClient: {ClientId} for TenantId={TenantId}",
                        request.DryRun ? "Would update" : "Updating", clientId, indicator.TenantId);

                    if (!request.DryRun)
                    {
                        AuthClientMappingExtensions.MapToClient(indicator, indicator.Tenant!, existingClient);
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

        foreach (AuthenticationClient orphan in existingClients.Values.Where(c => c.Id.StartsWith("ac:")))
        {
            deleted++;
            logger.LogWarning("{Action} orphaned AuthenticationClient: {ClientId}",
                request.DryRun ? "Would delete" : "Deleting", orphan.Id);

            if (!request.DryRun)
            {
                _ = authDb.AuthenticationClients.Remove(orphan);
            }
        }

        if (!request.DryRun)
        {
            _ = await authDb.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Resync complete (DryRun={DryRun}): Created={Created}, Updated={Updated}, Deleted={Deleted}, Unchanged={Unchanged}",
            request.DryRun, created, updated, deleted, unchanged);

        return new ResyncAuthenticationClientsResult(created, updated, deleted, unchanged);
    }
}
