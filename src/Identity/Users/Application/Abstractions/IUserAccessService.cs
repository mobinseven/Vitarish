using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Abstractions;
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
