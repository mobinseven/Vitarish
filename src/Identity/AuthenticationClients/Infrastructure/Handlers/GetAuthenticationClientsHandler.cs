using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.AuthenticationClients.Application.Queries;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Infrastructure.Extensions;

namespace Vitarish.AuthenticationClients.Infrastructure.Handlers;

public sealed class GetAuthenticationClientsHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<GetAuthenticationClients, IEnumerable<AuthenticationClient>>
{
    public async Task<IEnumerable<AuthenticationClient>> Handle(GetAuthenticationClients request, CancellationToken cancellationToken)
    {
        if (await AuthenticationClientCacheExtensions.GetClientsAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        AuthenticationClient[] clients = await db.AuthenticationClients.AsNoTracking().ToArrayAsync(cancellationToken);
        await AuthenticationClientCacheExtensions.SetClientsAsync(cache, clients, cancellationToken);
        return clients;
    }
}
