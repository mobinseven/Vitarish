using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class DisableTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher)
    : IRequestHandler<DisableTenantIndicator, Result>
{
    public async Task<Result> Handle(DisableTenantIndicator request, CancellationToken cancellationToken)
    {
        var indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return Result.Failure(Error.NotFound("TenantIndicator.NotFound", "Tenant indicator was not found."));
        }

        indicator.Disable();
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new Identity.Core.Parties.Domain.Events.TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);
        return Result.Success();
    }
}
