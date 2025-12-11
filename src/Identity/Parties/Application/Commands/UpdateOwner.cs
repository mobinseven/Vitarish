using FluentValidation;
using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Commands;

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
