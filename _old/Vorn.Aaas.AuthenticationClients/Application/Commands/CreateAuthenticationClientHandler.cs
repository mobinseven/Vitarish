using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Domain.Events;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

internal sealed class CreateAuthenticationClientHandler(AuthenticationClientDbContext db, IPublisher publisher) : IRequestHandler<CreateAuthenticationClient, AuthenticationClient?>
{
    public async Task<AuthenticationClient?> Handle(CreateAuthenticationClient request, CancellationToken cancellationToken)
    {
        if(await db.AuthenticationClients.AnyAsync(a => a.Id == request.Id, cancellationToken))
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

        db.AuthenticationClients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new AuthenticationClientCreatedEvent(client.Id), cancellationToken);
        return client;
    }
}
