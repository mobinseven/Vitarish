using MediatR;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Queries;

/// <summary>
/// Query to retrieve all <see cref="Owner"/> entities.
/// </summary>
public sealed record GetOwners : IRequest<IEnumerable<Owner>>;
