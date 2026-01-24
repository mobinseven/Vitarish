using FluentValidation;
using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to update an existing <see cref="Tenant"/>.
/// </summary>
public sealed record UpdateTenant(
    Guid Id,
    Guid OwnerId,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? ModifiedBy = null,
    Dictionary<string, object?> Metadata = default) : IRequest<Tenant?>;

/// <summary>
/// Validator for <see cref="UpdateTenant"/>.
/// </summary>
public class UpdateTenantValidator : AbstractValidator<UpdateTenant>
{
    public UpdateTenantValidator()
    {
        _ = RuleFor(c => c.Name).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
