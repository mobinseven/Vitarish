using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class CreateTenantHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateTenant, Tenant?>
{
    public async Task<Tenant?> Handle(CreateTenant request, CancellationToken cancellationToken)
    {
        // Verify owner exists
        if (!await db.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken))
        {
            return null;
        }

        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            ClientId = request.ClientId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _ = db.Tenants.Add(tenant);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantCreatedEvent(tenant.Id, tenant.OwnerId), cancellationToken);
        return tenant;
    }
}
