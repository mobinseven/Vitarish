using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to set a user's email address.
/// </summary>
public sealed record SetUserEmail(string UserId, string Email) : IRequest<Result>;
