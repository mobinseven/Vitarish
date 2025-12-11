using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class DisableTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher) : IRequestHandler<DisableTenantIndicator, bool>
{
    public async Task<bool> Handle(DisableTenantIndicator request, CancellationToken cancellationToken)
    {
        TenantIndicator? indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return false;
        }

        indicator.Status = IndicatorStatus.Disabled;
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);

        return true;
    }
}
