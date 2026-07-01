namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

/// <summary>
/// Configuration options for the Tamin (Social Security) insurance provider adapter.
/// </summary>
public sealed class TaminOptions
{
    public const string SectionName = "Prescription:Providers:Tamin";

    /// <summary>Base URL for the Tamin API.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API key or client identifier for Tamin.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Whether this provider is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether to use sandbox mode. Kept for backward-compatible configuration. Prefer <see cref="Endpoint" />.</summary>
    public bool UseSandbox { get; set; }

    /// <summary>Selected generated SDK endpoint surface.</summary>
    public TaminEndpointMode Endpoint { get; set; } = TaminEndpointMode.Production;

    /// <summary>Default page size for reference-data sync.</summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>Maximum page size accepted for reference-data calls.</summary>
    public int MaxPageSize { get; set; } = 500;

    /// <summary>Safety cap for all-page reference-data sync.</summary>
    public int MaxReferencePages { get; set; } = 1000;

    /// <summary>Whether reference-data operations require an authenticated session.</summary>
    public bool ReferenceDataRequiresToken { get; set; }

    /// <summary>Token-expiry skew used before re-authenticating.</summary>
    public TimeSpan TokenSkew { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>OAuth redirect URI for future PKCE onboarding flows.</summary>
    public string? OAuthRedirectUri { get; set; }

    /// <summary>Sign-out redirect URI for future provider logout flows.</summary>
    public string? SignOutRedirectUri { get; set; }

    /// <summary>Sandbox base URL (if different from production).</summary>
    public string? SandboxBaseUrl { get; set; }

    /// <summary>Resolves the effective endpoint based on explicit endpoint or legacy sandbox mode.</summary>
    public TaminEndpointMode EffectiveEndpoint => UseSandbox ? TaminEndpointMode.Sandbox : Endpoint;

    /// <summary>Resolves the effective base URL based on endpoint mode.</summary>
    public string EffectiveBaseUrl => EffectiveEndpoint == TaminEndpointMode.Sandbox && !string.IsNullOrWhiteSpace(SandboxBaseUrl)
        ? SandboxBaseUrl
        : BaseUrl;
}
