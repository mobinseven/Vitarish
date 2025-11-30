namespace Vitarish.Web.Clients.Options;

/// <summary>
/// Configuration options for identity/authentication settings.
/// </summary>
public class IdentityOptions
{
    /// <summary>
    /// The OIDC authority base URL.
    /// </summary>
    public required string Authority { get; set; }

    /// <summary>
    /// Client secret used for token requests and API calls.
    /// </summary>
    public required string Secret { get; set; }

    /// <summary>
    /// Registered OIDC client ID.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Indicates if running in local development mode.
    /// </summary>
    public bool? Local { get; set; }

    /// <summary>
    /// Indicates if running in development environment.
    /// </summary>
    public bool? Dev { get; set; }

    /// <summary>
    /// Domain for data protection (optional, inferred from Authority).
    /// </summary>
    public string? DataProtectionDomain { get; set; }

    /// <summary>
    /// Password for protecting data protection keys.
    /// </summary>
    public string? DataProtectionPassword { get; set; }

    /// <summary>
    /// Cookie expiration in days. Defaults to 14.
    /// </summary>
    public int CookieExpirationDays { get; set; } = 14;
}
