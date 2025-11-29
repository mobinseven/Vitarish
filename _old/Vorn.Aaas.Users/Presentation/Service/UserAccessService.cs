namespace Vorn.Aaas.Users.Presentation.Service;

using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.Users.Application.Commands;
using Vorn.Aaas.Users.Application.Queries;
using Vorn.Aaas.Users.ReadModel.Dtos;

/// <summary>
/// In‑process abstraction replacing the /api/users HTTP endpoints.
/// </summary>
public interface IUserAccessService
{
    /* ---------- Queries ---------- */

    Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(
        string userId,
        CancellationToken ct = default);

    Task<ClaimDto?> GetUserAccessClaimAsync(
        string userId,
        string clientId,
        CancellationToken ct = default);

    Task<bool> HasUserAccessAsync(
        string userId,
        string clientId,
        CancellationToken ct = default);

    Task<IEnumerable<UserDto>> GetClientUsersAsync(
        string clientId,
        CancellationToken ct = default);

    Task<UserDto?> GetClientUserAsync(
        string clientId,
        string userId,
        CancellationToken ct = default);

    /* ---------- Commands ---------- */

    Task SetUserAccessAsync(
        SetUserAccessClaimCommand command,
        CancellationToken ct = default);

    Task RemoveUserAccessAsync(
        RemoveUserAccessClaimCommand command,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class UserAccessService(ISender sender) : IUserAccessService
{

    /* ---------- Queries ---------- */

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

    /* ---------- Commands ---------- */

    public Task SetUserAccessAsync(
        SetUserAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);

    public Task RemoveUserAccessAsync(
        RemoveUserAccessClaimCommand command,
        CancellationToken ct = default) =>
        sender.Send(command, ct);
}
