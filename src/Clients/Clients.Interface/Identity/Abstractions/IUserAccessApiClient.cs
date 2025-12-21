using Vitarish.Requests.Domain.Models;

namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IUserAccessApiClient
{
    /* ---------- Request-Based Methods (New Pattern) ---------- */
    
    /// <summary>
    /// Check if a request has access based on full request context.
    /// This is the preferred method for access checks.
    /// </summary>
    Task<bool> HasRequestAccess(RequestIdentityDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get access claim for a request based on full request context.
    /// This is the preferred method for retrieving access claims.
    /// </summary>
    Task<ClaimDto?> GetRequestAccessClaim(RequestIdentityDto request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set access for a request based on full request context.
    /// This is the preferred method for granting access.
    /// </summary>
    Task SetRequestAccess(RequestIdentityDto request, string accessValue, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove access for a request based on full request context.
    /// This is the preferred method for revoking access.
    /// </summary>
    Task RemoveRequestAccess(RequestIdentityDto request, CancellationToken cancellationToken = default);

    /* ---------- Legacy Client-Based Methods (Deprecated) ---------- */
    
    Task<IEnumerable<ClaimDto>?> GetUserClaims(string userId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use GetRequestAccessClaim with RequestIdentityDto instead. This method will be removed in a future version.")]
    Task<ClaimDto?> GetUserAccessClaim(string userId, string clientId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use HasRequestAccess with RequestIdentityDto instead. This method will be removed in a future version.")]
    Task<bool> HasUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<UserDto>?> GetClientUsers(string clientId, CancellationToken cancellationToken = default);
    
    Task<UserDto?> GetClientUser(string clientId, string userId, CancellationToken cancellationToken = default);
    
    [Obsolete("Use SetRequestAccess with RequestIdentityDto instead. This method will be removed in a future version.")]
    Task SetUserAccess(string userId, string clientId, string accessValue, CancellationToken cancellationToken = default);
    
    [Obsolete("Use RemoveRequestAccess with RequestIdentityDto instead. This method will be removed in a future version.")]
    Task RemoveUserAccess(string userId, string clientId, CancellationToken cancellationToken = default);
}
