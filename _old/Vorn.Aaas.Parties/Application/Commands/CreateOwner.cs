
using FluentValidation;
using MediatR;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to create a new <see cref="Owner"/>.
/// </summary>
public sealed record CreateOwner(
    string Name,
    string? LogoUrl = null,
    string? Description = null,
    string? CreatedBy = null) : IRequest<Owner?>;

/// <summary>
/// Validator for <see cref="CreateOwner"/>.
/// </summary>
public class CreateOwnerValidator : AbstractValidator<CreateOwner>
{
    public CreateOwnerValidator()
    {
        _ = RuleFor(c => c.Name).NotEmpty();
    }
}

internal sealed class CreateOwnerHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateOwner, Owner?>
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
