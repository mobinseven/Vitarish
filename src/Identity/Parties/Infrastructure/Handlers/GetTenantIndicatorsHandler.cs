using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetTenantIndicatorsHandler(PartiesDbContext db)
    : IRequestHandler<GetTenantIndicators, IEnumerable<TenantIndicator>>
{
    public async Task<IEnumerable<TenantIndicator>> Handle(GetTenantIndicators request, CancellationToken ct) =>
        await db.TenantIndicators
            .Where(i => i.TenantId == request.TenantId)
            .OrderByDescending(i => i.IsPrimary)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
}
