using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Maps a doctor's credentials for a specific insurance provider at a specific clinic.
/// Credentials themselves are stored via secure secret references, never plaintext.
/// </summary>
public sealed class DoctorProviderCredential
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public required string ProviderKey { get; set; }
    public Guid ClinicId { get; set; }

    /// <summary>Provider-specific doctor identifier (e.g., medical council number mapping).</summary>
    public string? ProviderDoctorIdentifier { get; set; }

    /// <summary>Provider-specific username.</summary>
    public string? ProviderUsername { get; set; }

    /// <summary>Reference to encrypted credential secret (never store plaintext).</summary>
    public string? CredentialSecretRef { get; set; }

    /// <summary>Reference to encrypted token (never store plaintext).</summary>
    public string? TokenSecretRef { get; set; }

    /// <summary>Active provider session ID.</summary>
    public string? SessionId { get; set; }

    /// <summary>When the current token expires.</summary>
    public DateTimeOffset? TokenExpiresAt { get; set; }

    public CredentialStatus Status { get; set; } = CredentialStatus.Pending;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
