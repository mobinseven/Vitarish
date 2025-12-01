using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class DeleteTenantIndicatorHandler(PartiesDbContext db) : IRequestHandler<DeleteTenantIndicator, Result>
{
    public async Task<Result> Handle(DeleteTenantIndicator request, CancellationToken cancellationToken)
    {
        TenantIndicator? indicator = await db.TenantIndicators
            .FirstOrDefaultAsync(i => i.Id == request.IndicatorId && i.TenantId == request.TenantId, cancellationToken);

        if (indicator is null)
        {
            return Result.Failure(Error.NotFound("TenantIndicator.NotFound", "Tenant indicator was not found."));
        }

        _ = db.TenantIndicators.Remove(indicator);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
