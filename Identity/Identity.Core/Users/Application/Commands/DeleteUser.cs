using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to delete a user.
/// </summary>
public sealed record DeleteUser(string Id) : IRequest<Result>;
