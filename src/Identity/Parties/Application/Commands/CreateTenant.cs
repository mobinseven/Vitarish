using FluentValidation;
using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to create a new <see cref="Tenant"/>.
/// </summary>
public sealed record CreateTenant(
    Guid OwnerId,
    string Name,
    string ClientId,
    string? LogoUrl = null,
    string? Description = null,
    string? CreatedBy = null) : IRequest<Tenant?>;

/// <summary>
/// Validator for <see cref="CreateTenant"/>.
/// </summary>
public class CreateTenantValidator : AbstractValidator<CreateTenant>
{
    public CreateTenantValidator()
    {
        _ = RuleFor(c => c.Name).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
