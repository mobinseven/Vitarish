using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;

internal sealed class DeleteAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<DeleteAuthenticationClient>
{
    public async Task Handle(DeleteAuthenticationClient request, CancellationToken cancellationToken)
    {
        AuthenticationClient? client = await db.AuthenticationClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return;
        }

        _ = db.AuthenticationClients.Remove(client);
        _ = await db.SaveChangesAsync(cancellationToken);
        await AuthenticationClientCacheExtensions.RemoveClientAsync(cache, client.Id, cancellationToken);
        await AuthenticationClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
    }
}
