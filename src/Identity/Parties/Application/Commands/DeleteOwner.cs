using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to delete an <see cref="Domain.Entities.Owner"/>.
/// </summary>
public sealed record DeleteOwner(Guid Id) : IRequest;
