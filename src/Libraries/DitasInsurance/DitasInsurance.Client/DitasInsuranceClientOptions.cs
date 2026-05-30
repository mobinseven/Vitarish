namespace Vitarish.DitasInsurance.Client;

public sealed class DitasInsuranceClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseSandbox { get; set; }
    public string? SandboxBaseUrl { get; set; }

    public string EffectiveBaseUrl => UseSandbox && !string.IsNullOrWhiteSpace(SandboxBaseUrl)
        ? SandboxBaseUrl
        : BaseUrl;
}
