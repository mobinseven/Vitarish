using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Events;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;

internal sealed class CreateAuthenticationClientHandler(AuthenticationClientDbContext db, IPublisher publisher, IDistributedCache cache) : IRequestHandler<CreateAuthenticationClient, Result<AuthenticationClient>>
{
    public async Task<Result<AuthenticationClient>> Handle(CreateAuthenticationClient request, CancellationToken cancellationToken)
    {
        AuthenticationClient client = new(
            request.Id,
            request.Url,
            request.Title,
            request.Description,
            request.LogoUrl,
            request.AutoAccess);

        _ = db.AuthenticationClients.Add(client);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Handle race condition where another request created a client with the same ID
            return Result.Failure<AuthenticationClient>(Error.Conflict(
                "AuthenticationClient.AlreadyExists",
                $"An authentication client with ID '{request.Id}' already exists."));
        }

        await publisher.Publish(new AuthenticationClientCreatedEvent(client.Id), cancellationToken);
        await AuthenticationClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await AuthenticationClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);

        return Result.Success(client);
    }
}
