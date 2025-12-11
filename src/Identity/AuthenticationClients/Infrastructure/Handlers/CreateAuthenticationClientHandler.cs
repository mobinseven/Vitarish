using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Domain.Events;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Infrastructure.Extensions;

namespace Vitarish.AuthenticationClients.Infrastructure.Handlers;

public sealed class CreateAuthenticationClientHandler(AuthenticationClientDbContext db, IPublisher publisher, IDistributedCache cache) : IRequestHandler<CreateAuthenticationClient, AuthenticationClient?>
{
    public async Task<AuthenticationClient?> Handle(CreateAuthenticationClient request, CancellationToken cancellationToken)
    {
        if (await db.AuthenticationClients.AnyAsync(a => a.Id == request.Id, cancellationToken))
        {
            return default;
        }

        AuthenticationClient client = new()
        {
            Id = request.Id,
            Url = request.Url,
            Title = request.Title,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            AutoAccess = request.AutoAccess
        };

        _ = db.AuthenticationClients.Add(client);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new AuthenticationClientCreatedEvent(client.Id), cancellationToken);
        await AuthenticationClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await AuthenticationClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
        return client;
    }
}
