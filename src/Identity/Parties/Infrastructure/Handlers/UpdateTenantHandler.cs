using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class UpdateTenantHandler(PartiesDbContext db) : IRequestHandler<UpdateTenant, Tenant?>
{
    public async Task<Tenant?> Handle(UpdateTenant request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        // Verify owner exists if changed
        if (tenant.OwnerId != request.OwnerId && !await db.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken))
        {
            return null;
        }

        tenant.OwnerId = request.OwnerId;
        tenant.Name = request.Name;
        tenant.LogoUrl = request.LogoUrl;
        tenant.Description = request.Description;
        tenant.ClientId = request.ClientId;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = request.ModifiedBy;

        _ = await db.SaveChangesAsync(cancellationToken);
        return tenant;
    }
}
