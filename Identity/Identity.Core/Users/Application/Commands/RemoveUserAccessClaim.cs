using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Command to remove a user's access claim for a client.
/// </summary>
public sealed record RemoveUserAccessClaim(string UserId, string ClientId) : IRequest<Result>;
