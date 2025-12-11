using FluentValidation;
using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Commands;

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
