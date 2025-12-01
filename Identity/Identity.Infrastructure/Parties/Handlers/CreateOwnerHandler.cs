using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Events;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class CreateOwnerHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateOwner, Result<Owner>>
{
    public async Task<Result<Owner>> Handle(CreateOwner request, CancellationToken cancellationToken)
    {
        Owner owner = new(Guid.NewGuid(), request.Name, request.LogoUrl, request.Description, request.CreatedBy);

        _ = db.Owners.Add(owner);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new OwnerCreatedEvent(owner.Id), cancellationToken);
        return Result.Success(owner);
    }
}
