using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class DeleteTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher)
    : IRequestHandler<DeleteTenantIndicator, bool>
{
    public async Task<bool> Handle(DeleteTenantIndicator request, CancellationToken ct)
    {
        TenantIndicator? indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, ct);

        if (indicator is null)
        {
            return false;
        }

        _ = db.TenantIndicators.Remove(indicator);
        _ = await db.SaveChangesAsync(ct);

        await publisher.Publish(new TenantIndicatorDeletedEvent(indicator.TenantId, indicator.Id, indicator.Method, indicator.Value), ct);

        return true;
    }
}
