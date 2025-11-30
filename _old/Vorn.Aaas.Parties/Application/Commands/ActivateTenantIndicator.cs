
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to activate a tenant indicator.
/// </summary>
public sealed record ActivateTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;

internal sealed class ActivateTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<ActivateTenantIndicator, bool>
{
    public async Task<bool> Handle(ActivateTenantIndicator request, CancellationToken cancellationToken)
    {
        TenantIndicator? indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null || indicator.Status != IndicatorStatus.Verified)
        {
            return false;
        }

        indicator.Status = IndicatorStatus.Active;
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new Domain.Events.TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return true;
    }
}
