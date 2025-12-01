using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Events;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class CreateTenantHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateTenant, Result<Tenant>>
{
    public async Task<Result<Tenant>> Handle(CreateTenant request, CancellationToken cancellationToken)
    {
        if (!await db.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken))
        {
            return Result.Failure<Tenant>(Error.NotFound("Owner.NotFound", $"Owner with ID '{request.OwnerId}' was not found."));
        }

        Tenant tenant = new(Guid.NewGuid(), request.OwnerId, request.Name, request.ClientId, request.LogoUrl, request.Description, request.CreatedBy);

        _ = db.Tenants.Add(tenant);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantCreatedEvent(tenant.Id, tenant.OwnerId), cancellationToken);
        return Result.Success(tenant);
    }
}
