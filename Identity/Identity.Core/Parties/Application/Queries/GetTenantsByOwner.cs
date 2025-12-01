using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query to retrieve tenants for a specific owner.
/// </summary>
public sealed record GetTenantsByOwner(Guid OwnerId) : IRequest<IEnumerable<Tenant>>;

/// <summary>
/// Validator for <see cref="GetTenantsByOwner"/>.
/// </summary>
public sealed class GetTenantsByOwnerValidator : AbstractValidator<GetTenantsByOwner>
{
    public GetTenantsByOwnerValidator()
    {
        _ = RuleFor(q => q.OwnerId).NotEmpty();
    }
}
