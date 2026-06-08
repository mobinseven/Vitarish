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

    /// <summary>Whether to use sandbox mode.</summary>
    public bool UseSandbox { get; set; }

    /// <summary>Sandbox base URL (if different from production).</summary>
    public string? SandboxBaseUrl { get; set; }

    /// <summary>Resolves the effective base URL based on sandbox mode.</summary>
    public string EffectiveBaseUrl => UseSandbox && !string.IsNullOrWhiteSpace(SandboxBaseUrl)
        ? SandboxBaseUrl
        : BaseUrl;
}
