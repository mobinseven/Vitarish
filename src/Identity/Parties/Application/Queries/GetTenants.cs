using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities.
/// </summary>
public sealed record GetTenants : IRequest<IEnumerable<Tenant>>;
