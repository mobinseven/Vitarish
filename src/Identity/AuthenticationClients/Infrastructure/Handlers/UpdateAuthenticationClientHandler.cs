using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Infrastructure.Extensions;

namespace Vitarish.AuthenticationClients.Infrastructure.Handlers;

public sealed class UpdateAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<UpdateAuthenticationClient>
{
    public async Task Handle(UpdateAuthenticationClient request, CancellationToken cancellationToken)
    {
        AuthenticationClient? client = await db.AuthenticationClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return;
        }

        client.Url = request.Url;
        client.Title = request.Title;
        client.Description = request.Description;
        client.LogoUrl = request.LogoUrl;
        client.AutoAccess = request.AutoAccess;

        _ = await db.SaveChangesAsync(cancellationToken);
        await AuthenticationClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await AuthenticationClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
    }
}
