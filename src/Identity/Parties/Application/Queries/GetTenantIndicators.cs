using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query to get all indicators for a tenant.
/// </summary>
public sealed record GetTenantIndicators(Guid TenantId) : IRequest<IEnumerable<TenantIndicator>>;
