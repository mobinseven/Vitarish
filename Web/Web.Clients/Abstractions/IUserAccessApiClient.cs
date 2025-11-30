using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// API client for user access operations.
/// </summary>
public interface IUserAccessApiClient
{
    /// <summary>
    /// Gets all claims for a user.
    /// </summary>
    Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's access claim for a specific client.
    /// </summary>
    Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has access to a client.
    /// </summary>
    Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with access to a client.
    /// </summary>
    Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user with access to a client.
    /// </summary>
    Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a user's access claim.
    /// </summary>
    Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user's access claim.
    /// </summary>
    Task RemoveUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
}
