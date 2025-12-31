namespace Vitarish.Requests.Application;

/// <summary>
/// Constants used for request identity context resolution.
/// </summary>
/// <remarks>
/// The claim type constants (TenantIdClaim, OwnerIdClaim) are part of a shared contract
/// with the identity provider. These values must be kept in sync with the client-side
/// constants in <c>Vitarish.Client.Api.Identity.Constants.AaasConstants</c>.
/// </remarks>
public static class RequestIdentityConstants
{
    /// <summary>
    /// The claim type for tenant identifier.
    /// </summary>
    public const string TenantIdClaim = "tenant_id";

    /// <summary>
    /// The claim type for owner identifier.
    /// </summary>
    public const string OwnerIdClaim = "owner_id";

    /// <summary>
    /// The HTTP header name for request identifier.
    /// </summary>
    public const string RequestIdHeader = "X-Request-Id";
}
