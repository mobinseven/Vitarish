namespace Vitarish.Prescription.Infrastructure.Providers.Salamat;

/// <summary>
/// Configuration options for the Salamat (Ministry of Health) insurance provider adapter.
/// </summary>
public sealed class SalamatOptions
{
    public const string SectionName = "Prescription:Providers:Salamat";

    /// <summary>Base URL for the Salamat API.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Client ID for OAuth2 authentication with Salamat.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret for OAuth2 authentication with Salamat.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Whether this provider is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether to use sandbox mode.</summary>
    public bool UseSandbox { get; set; }

    /// <summary>Sandbox base URL (if different from production).</summary>
    public string? SandboxBaseUrl { get; set; }
}
