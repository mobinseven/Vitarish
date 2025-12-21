using Vitarish.Requests.Domain.Models;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Abstractions;
/// <summary>
/// In‑process abstraction replacing the /api/users HTTP endpoints.
/// </summary>
public interface IUserAccessService
{
    /* ---------- Request-Based Methods (New Pattern) ---------- */

    /// <summary>
    /// Check if a request has access based on the full request context.
    /// This is the preferred method for access checks.
    /// </summary>
    Task<bool> HasRequestAccessAsync(
        RequestIdentityDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Get access claim for a request based on the full request context.
    /// This is the preferred method for retrieving access claims.
    /// </summary>
    Task<ClaimDto?> GetRequestAccessClaimAsync(
        RequestIdentityDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Set access claim for a request based on the full request context.
    /// This is the preferred method for granting access.
    /// </summary>
    Task SetRequestAccessAsync(
        SetRequestAccessClaimCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Remove access claim for a request based on the full request context.
    /// This is the preferred method for revoking access.
    /// </summary>
    Task RemoveRequestAccessAsync(
        RemoveRequestAccessClaimCommand command,
        CancellationToken ct = default);

    /* ---------- Legacy Client-Based Methods (Deprecated) ---------- */

    Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(
        string userId,
        CancellationToken ct = default);

    [Obsolete("Use HasRequestAccessAsync with RequestIdentityDto instead. This method will be removed in a future version.")]
    Task<ClaimDto?> GetUserAccessClaimAsync(
        string userId,
        string clientId,
        CancellationToken ct = default);

    [Obsolete("Use HasRequestAccessAsync with RequestIdentityDto instead. This method will be removed in a future version.")]
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

    /* ---------- Legacy Commands (Deprecated) ---------- */

    [Obsolete("Use SetRequestAccessAsync with SetRequestAccessClaimCommand instead. This method will be removed in a future version.")]
    Task SetUserAccessAsync(
        SetUserAccessClaimCommand command,
        CancellationToken ct = default);

    [Obsolete("Use RemoveRequestAccessAsync with RemoveRequestAccessClaimCommand instead. This method will be removed in a future version.")]
    Task RemoveUserAccessAsync(
        RemoveUserAccessClaimCommand command,
        CancellationToken ct = default);
}
