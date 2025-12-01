using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Enums;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class ActivateTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher)
    : IRequestHandler<ActivateTenantIndicator, Result>
{
    public async Task<Result> Handle(ActivateTenantIndicator request, CancellationToken cancellationToken)
    {
        var indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return Result.Failure(Error.NotFound("TenantIndicator.NotFound", "Tenant indicator was not found."));
        }

        if (indicator.Status != IndicatorStatus.Verified)
        {
            return Result.Failure(Error.Conflict("TenantIndicator.NotVerified", "Indicator must be verified before activation."));
        }

        indicator.Activate();
        _ = await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new Identity.Core.Parties.Domain.Events.TenantIndicatorUpdatedEvent(indicator.TenantId, indicator.Id), cancellationToken);
        return Result.Success();
    }
}
