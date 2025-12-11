using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class ActivateTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<ActivateTenantIndicator, bool>
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

        await publisher.Publish(new TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return true;
    }
}
