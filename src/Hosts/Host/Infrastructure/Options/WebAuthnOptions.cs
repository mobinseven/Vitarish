namespace Vitarish.Host.Infrastructure.Options;

/// <summary>
/// Configuration options for WebAuthn/FIDO2 authentication.
/// </summary>
public class WebAuthnOptions
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "WebAuthn";

    /// <summary>
    /// The relying party name (e.g., your application name).
    /// This is displayed to the user during registration/authentication.
    /// </summary>
    public string ServerName { get; set; } = "Vitarish";

    /// <summary>
    /// The relying party ID (domain name, e.g., "example.com").
    /// Must match the domain where the application is hosted.
    /// </summary>
    public required string ServerDomain { get; set; }

    /// <summary>
    /// The origin URL(s) allowed for WebAuthn operations.
    /// Should include the protocol and domain (e.g., "https://example.com").
    /// </summary>
    public required HashSet<string> Origins { get; set; }

    /// <summary>
    /// Timeout in milliseconds for registration and authentication ceremonies.
    /// Default is 60000 (60 seconds).
    /// </summary>
    public uint Timeout { get; set; } = 60000;

    /// <summary>
    /// The attestation conveyance preference.
    /// Options: "none", "indirect", "direct", "enterprise".
    /// Default is "none" (no attestation).
    /// </summary>
    public string AttestationConveyance { get; set; } = "none";

    /// <summary>
    /// The authenticator attachment preference.
    /// Options: "platform" (built-in authenticator like Touch ID),
    /// "cross-platform" (external authenticator like YubiKey), or null (no preference).
    /// </summary>
    public string? AuthenticatorAttachment { get; set; }

    /// <summary>
    /// Whether to require user verification (PIN, biometric, etc.) during authentication.
    /// Options: "required", "preferred", "discouraged".
    /// Default is "preferred".
    /// </summary>
    public string UserVerification { get; set; } = "preferred";

    /// <summary>
    /// Whether to require resident keys (discoverable credentials).
    /// Options: "required", "preferred", "discouraged".
    /// Default is "discouraged".
    /// </summary>
    public string RequireResidentKey { get; set; } = "discouraged";
}
