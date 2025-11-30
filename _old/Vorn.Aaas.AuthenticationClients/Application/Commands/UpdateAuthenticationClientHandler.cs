
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Extensions;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

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
