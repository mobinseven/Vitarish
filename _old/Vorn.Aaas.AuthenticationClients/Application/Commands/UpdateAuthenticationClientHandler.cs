using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

internal sealed class UpdateAuthenticationClientHandler(AuthenticationClientDbContext db) : IRequestHandler<UpdateAuthenticationClient>
{
    public async Task Handle(UpdateAuthenticationClient request, CancellationToken cancellationToken)
    {
        AuthenticationClient? client = await db.AuthenticationClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if(client is null)
        {
            return;
        }

        client.Url = request.Url;
        client.Title = request.Title;
        client.Description = request.Description;
        client.LogoUrl = request.LogoUrl;
        client.AutoAccess = request.AutoAccess;

        await db.SaveChangesAsync(cancellationToken);
    }
}
