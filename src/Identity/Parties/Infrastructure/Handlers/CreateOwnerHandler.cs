using MediatR;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class CreateOwnerHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateOwner, Owner?>
{
    public async Task<Owner?> Handle(CreateOwner request, CancellationToken cancellationToken)
    {
        Owner owner = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _ = db.Owners.Add(owner);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new OwnerCreatedEvent(owner.Id), cancellationToken);
        return owner;
    }
}
