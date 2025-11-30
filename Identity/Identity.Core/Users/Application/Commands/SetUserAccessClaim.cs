using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to set a user's access claim for a client.
/// </summary>
public sealed record SetUserAccessClaim(string UserId, string ClientId, string AccessValue) : IRequest<Result>;
