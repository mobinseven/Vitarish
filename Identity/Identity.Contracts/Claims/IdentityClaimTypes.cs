namespace Vitarish.Identity.Contracts.Claims;

/// <summary>
/// Standard claim types used in the Identity module.
/// These replace the legacy RequestIdentities approach with standard claims-based access.
/// </summary>
public static class IdentityClaimTypes
{
    /// <summary>
    /// Claim prefix used for all identity claims.
    /// </summary>
    public const string Prefix = "urn:vitarish:identity:";

    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public const string UserId = Prefix + "userid";

    /// <summary>
    /// The owner's unique identifier.
    /// </summary>
    public const string OwnerId = Prefix + "ownerid";

    /// <summary>
    /// The tenant's unique identifier.
    /// </summary>
    public const string TenantId = Prefix + "tenantid";

    /// <summary>
    /// The authentication client's identifier.
    /// </summary>
    public const string ClientId = Prefix + "clientid";

    /// <summary>
    /// The user's access level for a specific client.
    /// </summary>
    public const string AccessLevel = Prefix + "accesslevel";

    /// <summary>
    /// Standard OpenID Connect subject claim.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Standard OpenID Connect name claim.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// Standard OpenID Connect email claim.
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// Standard OpenID Connect email_verified claim.
    /// </summary>
    public const string EmailVerified = "email_verified";
}
