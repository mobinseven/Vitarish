using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class DeleteTenantHandler(PartiesDbContext db) : IRequestHandler<DeleteTenant>
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
