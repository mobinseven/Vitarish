
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
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

internal sealed class CreateTenantHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<CreateTenant, Tenant?>
{
    public async Task<Tenant?> Handle(CreateTenant request, CancellationToken cancellationToken)
    {
        // Verify owner exists
        if (!await db.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken))
        {
            return null;
        }

        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            ClientId = request.ClientId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _ = db.Tenants.Add(tenant);
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantCreatedEvent(tenant.Id, tenant.OwnerId), cancellationToken);
        return tenant;
    }
}
