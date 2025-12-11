using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Extensions;

namespace Vitarish.ApiClients.Infrastructure.Handlers;

public sealed class DeleteApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<DeleteApiClient>
{
    public async Task Handle(DeleteApiClient request, CancellationToken cancellationToken)
    {
        ApiClient? client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return;
        }

        _ = db.ApiClients.Remove(client);
        _ = await db.SaveChangesAsync(cancellationToken);
        await ApiClientCacheExtensions.RemoveClientAsync(cache, client.Id, cancellationToken);
        await ApiClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
    }
}
