using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Command to delete an owner.
/// </summary>
public sealed record DeleteOwner(Guid Id) : IRequest<Result>;
