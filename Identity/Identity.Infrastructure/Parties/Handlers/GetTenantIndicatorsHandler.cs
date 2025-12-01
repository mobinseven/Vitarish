using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetTenantIndicatorsHandler(PartiesDbContext db) : IRequestHandler<GetTenantIndicators, IEnumerable<TenantIndicator>>
{
    public async Task<IEnumerable<TenantIndicator>> Handle(GetTenantIndicators request, CancellationToken cancellationToken)
    {
        return await db.TenantIndicators
            .AsNoTracking()
            .Where(i => i.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
    }
}
