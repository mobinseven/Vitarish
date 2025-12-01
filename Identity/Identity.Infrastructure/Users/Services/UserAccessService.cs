using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <inheritdoc />
public sealed class UserAccessService(ISender sender) : IUserAccessService
{
    /* ---------- Queries ---------- */

    public Task<bool> HasUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new HasUserAccess(userId, clientId), ct);

    public Task<IEnumerable<User>> GetClientUsersAsync(
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new GetClientUsers(clientId), ct);

    public Task<User?> GetClientUserAsync(
        string clientId,
        string userId,
        CancellationToken ct = default) =>
        sender.Send(new GetClientUser(clientId, userId), ct);

    /* ---------- Commands ---------- */

    public Task<Result> SetUserAccessAsync(
        string userId,
        string clientId,
        string accessValue,
        CancellationToken ct = default) =>
        sender.Send(new SetUserAccessClaim(userId, clientId, accessValue), ct);

    public Task<Result> RemoveUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new RemoveUserAccessClaim(userId, clientId), ct);
}
