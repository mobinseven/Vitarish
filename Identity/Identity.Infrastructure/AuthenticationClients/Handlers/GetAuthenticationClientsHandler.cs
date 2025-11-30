using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.AuthenticationClients.Application.Queries;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;

internal sealed class GetAuthenticationClientsHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<GetAuthenticationClients, IEnumerable<AuthenticationClient>>
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
