using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;

/// <summary>
/// Query to get all indicators for a tenant.
/// </summary>
public sealed record GetTenantIndicators(Guid TenantId) : IRequest<IEnumerable<TenantIndicator>>;

internal sealed class GetTenantIndicatorsHandler(PartiesDbContext db)
    : IRequestHandler<GetTenantIndicators, IEnumerable<TenantIndicator>>
{
    public async Task<IEnumerable<TenantIndicator>> Handle(GetTenantIndicators request, CancellationToken ct) =>
        await db.TenantIndicators
            .Where(i => i.TenantId == request.TenantId)
            .OrderByDescending(i => i.IsPrimary)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
}
