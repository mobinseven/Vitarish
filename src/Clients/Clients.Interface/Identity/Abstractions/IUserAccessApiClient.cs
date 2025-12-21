using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IUserAccessApiClient
{
    /* ---------- Request-Based Methods (New Pattern) ---------- */
    
    /// <summary>
    /// Check if a request has access based on full request context.
    /// This is the preferred method for access checks.
    /// </summary>
    Task<bool> HasRequestAccess(RequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get access claim for a request based on full request context.
    /// This is the preferred method for retrieving access claims.
    /// </summary>
    Task<ClaimDto?> GetRequestAccessClaim(RequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set access for a request based on full request context.
    /// This is the preferred method for granting access.
    /// </summary>
    Task SetRequestAccess(RequestDto request, string accessValue, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove access for a request based on full request context.
    /// This is the preferred method for revoking access.
    /// </summary>
    Task RemoveRequestAccess(RequestDto request, CancellationToken cancellationToken = default);

    /* ---------- Legacy Client-Based Methods (Deprecated) ---------- */
    
    Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use GetRequestAccessClaim with RequestDto instead. This method will be removed in a future version.")]
    Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use HasRequestAccess with RequestDto instead. This method will be removed in a future version.")]
    Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default);
    
    Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use SetRequestAccess with RequestDto instead. This method will be removed in a future version.")]
    Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default);
    
    [Obsolete("Use RemoveRequestAccess with RequestDto instead. This method will be removed in a future version.")]
    Task RemoveUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
}
