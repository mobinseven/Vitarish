using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query for a single <see cref="Tenant"/> by identifier.
/// </summary>
public sealed record GetTenant(Guid Id) : IRequest<Tenant?>;

/// <summary>
/// Validator for <see cref="GetTenant"/>.
/// </summary>
public sealed class GetTenantValidator : AbstractValidator<GetTenant>
{
    public GetTenantValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
