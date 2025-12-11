using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.ApiClients.Application.Queries;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Extensions;

namespace Vitarish.ApiClients.Infrastructure.Handlers;

public sealed class GetApiClientsHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<GetApiClients, IEnumerable<ApiClient>>
{
    public async Task<IEnumerable<ApiClient>> Handle(GetApiClients request, CancellationToken cancellationToken)
    {
        if (await ApiClientCacheExtensions.GetClientsAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        ApiClient[] clients = await db.ApiClients.AsNoTracking().ToArrayAsync(cancellationToken);
        await ApiClientCacheExtensions.SetClientsAsync(cache, clients, cancellationToken);
        return clients;
    }
}
