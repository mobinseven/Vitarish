using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Infrastructure.Extensions;

namespace Vitarish.AuthenticationClients.Infrastructure.Handlers;

public sealed class DeleteAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<DeleteAuthenticationClient>
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
