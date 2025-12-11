using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Extensions;

namespace Vitarish.ApiClients.Infrastructure.Handlers;

public sealed class UpdateApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<UpdateApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(UpdateApiClient request, CancellationToken cancellationToken)
    {
        ApiClient? client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return null;
        }

        client.Secret = request.Secret;
        client.Scopes = request.Scopes;
        client.HostUri = request.HostUri;

        _ = await db.SaveChangesAsync(cancellationToken);
        await ApiClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await ApiClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
        return client;
    }
}
