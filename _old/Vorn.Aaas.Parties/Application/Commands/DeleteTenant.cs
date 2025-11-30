using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to delete a <see cref="Domain.Entities.Tenant"/>.
/// </summary>
public sealed record DeleteTenant(Guid Id) : IRequest;

internal sealed class DeleteTenantHandler(PartiesDbContext db) : IRequestHandler<DeleteTenant>
{
    public async Task Handle(DeleteTenant request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        _ = db.Tenants.Remove(tenant);
        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
