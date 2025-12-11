using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query for a single <see cref="Tenant"/> by identifier.
/// </summary>
public sealed record GetTenant(Guid Id) : IRequest<Tenant?>;
