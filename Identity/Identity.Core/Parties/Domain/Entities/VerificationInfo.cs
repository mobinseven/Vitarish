using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Core.Parties.Domain.Entities;

/// <summary>
/// Verification metadata for a tenant indicator.
/// </summary>
public sealed class VerificationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationInfo"/> class.
    /// </summary>
    public VerificationInfo(IndicatorVerificationMethod method, string evidence, string evidenceExpectedHash, string? token = null)
    {
        Method = method;
        Evidence = evidence;
        EvidenceExpectedHash = evidenceExpectedHash;
        Token = token;
    }

    /// <summary>Verification method used.</summary>
    public IndicatorVerificationMethod Method { get; }

    /// <summary>Evidence location (e.g., _verify.app.tenant.com).</summary>
    public string Evidence { get; }

    /// <summary>Hash of the expected verification token.</summary>
    public string EvidenceExpectedHash { get; }

    /// <summary>
    /// Actual verification token (plaintext, for display to user).
    /// This token must be added to the DNS TXT record for domain verification.
    /// <para>
    /// <b>Security note:</b> This value is sensitive. It should only be displayed to authorized users
    /// who control the domain being verified. <b>Do not log, persist, or expose this token to unauthorized parties.</b>
    /// Leaking this token may allow unauthorized users to complete domain verification.
    /// Only populated for DNS TXT verification method.
    /// </para>
    /// </summary>
    public string? Token { get; }
}
