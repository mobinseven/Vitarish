namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Ditas;

/// <summary>
/// Configuration options for the DITAS (Armed Forces Insurance) provider adapter.
/// </summary>
public sealed class DitasOptions
{
    public const string SectionName = "Prescription:Providers:Ditas";

    /// <summary>Base URL for the DITAS gateway API.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API key or client identifier for DITAS.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Whether this provider is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether to use sandbox mode.</summary>
    public bool UseSandbox { get; set; }

    /// <summary>Sandbox base URL (if different from production).</summary>
    public string? SandboxBaseUrl { get; set; }
}
