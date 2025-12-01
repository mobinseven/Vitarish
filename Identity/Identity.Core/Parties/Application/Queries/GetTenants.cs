using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Queries;

/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities.
/// </summary>
public sealed record GetTenants : IRequest<IEnumerable<Tenant>>;
