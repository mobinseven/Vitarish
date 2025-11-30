namespace Vitarish.Identity.Core;

/// <summary>
/// Constants used across the Identity module.
/// </summary>
public static class IdentityConstants
{
    /// <summary>
    /// The admin role name.
    /// </summary>
    public const string Admin = nameof(Admin);

    /// <summary>
    /// The claim type for access claims.
    /// </summary>
    public const string AccessClaim = "access";

    /// <summary>
    /// The scope for access-related claims.
    /// </summary>
    public const string AccessScope = "access";

    /// <summary>
    /// Standard claim types.
    /// </summary>
    public static class ClaimTypes
    {
        /// <summary>
        /// The subject claim type (user id).
        /// </summary>
        public const string Subject = "sub";

        /// <summary>
        /// The client id claim type.
        /// </summary>
        public const string ClientId = "client_id";

        /// <summary>
        /// The given name claim type.
        /// </summary>
        public const string GivenName = "given_name";

        /// <summary>
        /// The family name claim type.
        /// </summary>
        public const string FamilyName = "family_name";

        /// <summary>
        /// The email claim type.
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// The tenant id claim type.
        /// </summary>
        public const string TenantId = "tenant_id";

        /// <summary>
        /// The owner id claim type.
        /// </summary>
        public const string OwnerId = "owner_id";

        /// <summary>
        /// The name claim type.
        /// </summary>
        public const string Name = "name";
    }
}
