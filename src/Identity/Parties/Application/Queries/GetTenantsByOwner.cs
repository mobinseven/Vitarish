using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities for a specific owner.
/// </summary>
public sealed record GetTenantsByOwner(Guid OwnerId) : IRequest<IEnumerable<Tenant>>;
