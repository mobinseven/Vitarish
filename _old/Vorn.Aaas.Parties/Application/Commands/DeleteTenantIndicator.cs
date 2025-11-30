using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Events;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;

/// <summary>
/// Command to delete a tenant indicator.
/// </summary>
public sealed record DeleteTenantIndicator(Guid TenantId, Guid IndicatorId) : IRequest<bool>;

internal sealed class DeleteTenantIndicatorHandler(PartiesDbContext db, IPublisher publisher)
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
