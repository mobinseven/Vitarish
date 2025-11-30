using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Service for managing user access and roles.
/// </summary>
public interface IAccessService
{
    /// <summary>
    /// Initializes the access service for the current user.
    /// </summary>
    /// <param name="clientId">Optional client ID override.</param>
    Task InitializeAsync(string? clientId = null);

    /// <summary>
    /// Renames a role across all users.
    /// </summary>
    void RenameRole(string roleName, string roleNewName);

    /// <summary>
    /// Current user's access configuration.
    /// </summary>
    Access Access { get; }

    /// <summary>
    /// Available roles.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// All users with access to the client.
    /// </summary>
    IReadOnlyList<UserDto> Users { get; }

    /// <summary>
    /// Current authenticated user.
    /// </summary>
    UserDto? User { get; }

    /* ---------- Queries ---------- */

    /// <summary>
    /// Gets all claims for a user.
    /// </summary>
    Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a user's access claim for a specific client.
    /// </summary>
    Task<ClaimDto?> GetUserAccessClaimAsync(string userId, string clientId, CancellationToken ct = default);

    /// <summary>
    /// Gets all access claims for a user matching a prefix.
    /// </summary>
    Task<IEnumerable<ClaimDto>> GetUserAccessClaimsAsync(string userId, string claimPrefix, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has access to a client.
    /// </summary>
    Task<bool> HasUserAccessAsync(string userId, string clientId, CancellationToken ct = default);

    /// <summary>
    /// Gets all users with access to a client.
    /// </summary>
    Task<IEnumerable<UserDto>> GetClientUsersAsync(string clientId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific user with access to a client.
    /// </summary>
    Task<UserDto?> GetClientUserAsync(string clientId, string userId, CancellationToken ct = default);

    /* ---------- Commands ---------- */

    /// <summary>
    /// Sets or updates a user's access claim.
    /// </summary>
    Task SetUserAccessAsync(string userId, string clientId, string accessValue, CancellationToken ct = default);

    /// <summary>
    /// Removes a user's access claim.
    /// </summary>
    Task RemoveUserAccessAsync(string userId, string clientId, CancellationToken ct = default);
}
