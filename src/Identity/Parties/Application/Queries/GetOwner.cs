using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query for a single <see cref="Owner"/> by identifier.
/// </summary>
public sealed record GetOwner(Guid Id) : IRequest<Owner?>;
