
using MediatR;
using Vitarish.Requests.Domain.Models;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Infrastructure.Service;
/// <inheritdoc />
public sealed class UserAccessService(ISender sender) : IUserAccessService
{
    /* ---------- Request-Based Methods (New Pattern) ---------- */

    public Task<bool> HasRequestAccessAsync(
        RequestIdentityDto request,
        CancellationToken ct = default) =>
        sender.Send(new HasRequestAccessQuery(request), ct);

    public Task<ClaimDto?> GetRequestAccessClaimAsync(
        RequestIdentityDto request,
        CancellationToken ct = default) =>
        sender.Send(new GetRequestAccessClaimQuery(request), ct);

    public Task SetRequestAccessAsync(
        SetRequestAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task RemoveRequestAccessAsync(
        RemoveRequestAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);

    /* ---------- Legacy Client-Based Methods (Deprecated) ---------- */

    public Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(
        string userId,
        CancellationToken ct = default) =>
        sender.Send(new GetUserClaimsQuery(userId), ct);

    public Task<ClaimDto?> GetUserAccessClaimAsync(
        string userId,
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new GetUserAccessClaimQuery(userId, clientId), ct);

    public Task<bool> HasUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new HasUserAccessQuery(userId, clientId), ct);

    public Task<IEnumerable<UserDto>> GetClientUsersAsync(
        string clientId,
        CancellationToken ct = default) =>
        sender.Send(new GetClientUsersQuery(clientId), ct);

    public Task<UserDto?> GetClientUserAsync(
        string clientId,
        string userId,
        CancellationToken ct = default) =>
        sender.Send(new GetClientUserQuery(clientId, userId), ct);

    /* ---------- Legacy Commands (Deprecated) ---------- */

    public Task SetUserAccessAsync(
        SetUserAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task RemoveUserAccessAsync(
        RemoveUserAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);
}
