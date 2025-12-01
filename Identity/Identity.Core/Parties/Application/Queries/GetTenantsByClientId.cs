using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query to retrieve tenants by client identifier.
/// </summary>
public sealed record GetTenantsByClientId(string ClientId) : IRequest<IEnumerable<Tenant>>;

/// <summary>
/// Validator for <see cref="GetTenantsByClientId"/>.
/// </summary>
public sealed class GetTenantsByClientIdValidator : AbstractValidator<GetTenantsByClientId>
{
    public GetTenantsByClientIdValidator()
    {
        _ = RuleFor(q => q.ClientId).NotEmpty();
    }
}
