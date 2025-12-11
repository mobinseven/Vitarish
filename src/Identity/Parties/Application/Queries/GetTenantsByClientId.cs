using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities for a specific client.
/// </summary>
public sealed record GetTenantsByClientId(string ClientId) : IRequest<IEnumerable<Tenant>>;
