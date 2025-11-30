using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.AuthenticationClients.Application.Queries;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;

internal sealed class GetAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<GetAuthenticationClient, AuthenticationClient?>
{
    public async Task<AuthenticationClient?> Handle(GetAuthenticationClient request, CancellationToken cancellationToken)
    {
        if (await AuthenticationClientCacheExtensions.GetClientAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        AuthenticationClient? client = await db.AuthenticationClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is not null)
        {
            await AuthenticationClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        }

        return client;
    }
}
