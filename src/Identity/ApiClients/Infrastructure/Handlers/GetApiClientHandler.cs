using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.ApiClients.Application.Queries;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Extensions;

namespace Vitarish.ApiClients.Infrastructure.Handlers;

public sealed class GetApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<GetApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(GetApiClient request, CancellationToken cancellationToken)
    {
        if (await ApiClientCacheExtensions.GetClientAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        ApiClient? entity = await db.ApiClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (entity is not null)
        {
            await ApiClientCacheExtensions.SetClientAsync(cache, entity, cancellationToken);
        }

        return entity;
    }
}
