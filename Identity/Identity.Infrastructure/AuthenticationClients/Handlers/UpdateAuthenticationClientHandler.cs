using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;

internal sealed class UpdateAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<UpdateAuthenticationClient>
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
