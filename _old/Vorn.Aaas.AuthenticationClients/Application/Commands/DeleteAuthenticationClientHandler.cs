using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

internal sealed class DeleteAuthenticationClientHandler(AuthenticationClientDbContext db) : IRequestHandler<DeleteAuthenticationClient>
{
    public async Task Handle(DeleteAuthenticationClient request, CancellationToken cancellationToken)
    {
        AuthenticationClient? client = await db.AuthenticationClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if(client is null)
        {
            return;
        }

        db.AuthenticationClients.Remove(client);
        await db.SaveChangesAsync(cancellationToken);
    }
}
