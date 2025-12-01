using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query for a single <see cref="Owner"/> by identifier.
/// </summary>
public sealed record GetOwner(Guid Id) : IRequest<Owner?>;

/// <summary>
/// Validator for <see cref="GetOwner"/>.
/// </summary>
public sealed class GetOwnerValidator : AbstractValidator<GetOwner>
{
    public GetOwnerValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
