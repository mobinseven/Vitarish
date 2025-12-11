using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Domain.Events;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Extensions;

namespace Vitarish.ApiClients.Infrastructure.Handlers;

public sealed class CreateApiClientHandler(ApiClientDbContext db, IPublisher publisher, IDistributedCache cache) : IRequestHandler<CreateApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(CreateApiClient request, CancellationToken cancellationToken)
    {
        if (await db.ApiClients.AnyAsync(a => a.Id == request.Id, cancellationToken))
        {
            return null;
        }

        ApiClient client = new()
        {
            Id = request.Id,
            Secret = request.Secret,
            Scopes = request.Scopes,
            HostUri = request.HostUri
        };

        _ = db.ApiClients.Add(client);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new ApiClientCreatedEvent(client.Id), cancellationToken);
        await ApiClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await ApiClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
        return client;
    }
}
