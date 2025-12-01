using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query to retrieve indicators for a tenant.
/// </summary>
public sealed record GetTenantIndicators(Guid TenantId) : IRequest<IEnumerable<TenantIndicator>>;

/// <summary>
/// Validator for <see cref="GetTenantIndicators"/>.
/// </summary>
public sealed class GetTenantIndicatorsValidator : AbstractValidator<GetTenantIndicators>
{
    public GetTenantIndicatorsValidator()
    {
        _ = RuleFor(q => q.TenantId).NotEmpty();
    }
}
