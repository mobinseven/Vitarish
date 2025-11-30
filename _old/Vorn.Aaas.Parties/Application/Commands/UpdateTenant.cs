
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
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
    string? ModifiedBy = null) : IRequest<Tenant?>;

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

internal sealed class UpdateTenantHandler(PartiesDbContext db) : IRequestHandler<UpdateTenant, Tenant?>
{
    public async Task<Tenant?> Handle(UpdateTenant request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        // Verify owner exists if changed
        if (tenant.OwnerId != request.OwnerId && !await db.Owners.AnyAsync(o => o.Id == request.OwnerId, cancellationToken))
        {
            return null;
        }

        tenant.OwnerId = request.OwnerId;
        tenant.Name = request.Name;
        tenant.LogoUrl = request.LogoUrl;
        tenant.Description = request.Description;
        tenant.ClientId = request.ClientId;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = request.ModifiedBy;

        _ = await db.SaveChangesAsync(cancellationToken);
        return tenant;
    }
}
