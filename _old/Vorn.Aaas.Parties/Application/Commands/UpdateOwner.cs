
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to update an existing <see cref="Owner"/>.
/// </summary>
public sealed record UpdateOwner(
    Guid Id,
    string Name,
    string? LogoUrl = null,
    string? Description = null,
    string? ModifiedBy = null) : IRequest<Owner?>;

/// <summary>
/// Validator for <see cref="UpdateOwner"/>.
/// </summary>
public class UpdateOwnerValidator : AbstractValidator<UpdateOwner>
{
    public UpdateOwnerValidator()
    {
        _ = RuleFor(c => c.Name).NotEmpty();
    }
}

internal sealed class UpdateOwnerHandler(PartiesDbContext db) : IRequestHandler<UpdateOwner, Owner?>
{
    public async Task<Owner?> Handle(UpdateOwner request, CancellationToken cancellationToken)
    {
        Owner? owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (owner is null)
        {
            return null;
        }

        owner.Name = request.Name;
        owner.LogoUrl = request.LogoUrl;
        owner.Description = request.Description;
        owner.ModifiedAt = DateTime.UtcNow;
        owner.ModifiedBy = request.ModifiedBy;

        _ = await db.SaveChangesAsync(cancellationToken);
        return owner;
    }
}
