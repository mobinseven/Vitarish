namespace Vitarish.Identity.Core.Abstractions;

/// <summary>
/// Provides access to the current user's identity information
/// extracted from claims. This replaces the legacy RequestIdentity pattern
/// with a claims-based approach.
/// </summary>
public interface IIdentityContext
{
    /// <summary>
    /// Gets the current user's unique identifier (sub claim).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the owner identifier for the current request context.
    /// </summary>
    string? OwnerId { get; }

    /// <summary>
    /// Gets the client identifier for the current request.
    /// </summary>
    string? ClientId { get; }

    /// <summary>
    /// Gets the tenant identifier for the current request, if multi-tenant.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Indicates whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Checks if the current user has a specific claim type with the specified value.
    /// </summary>
    /// <param name="claimType">The type of claim to check.</param>
    /// <param name="claimValue">The expected value of the claim.</param>
    /// <returns>True if the user has the claim with the specified value.</returns>
    bool HasClaim(string claimType, string claimValue);

    /// <summary>
    /// Gets the value of a claim by type.
    /// </summary>
    /// <param name="claimType">The type of claim to retrieve.</param>
    /// <returns>The claim value, or null if not found.</returns>
    string? GetClaimValue(string claimType);

    /// <summary>
    /// Gets all values for a claim type.
    /// </summary>
    /// <param name="claimType">The type of claim to retrieve.</param>
    /// <returns>All values for the specified claim type.</returns>
    IEnumerable<string> GetClaimValues(string claimType);
}
