using System.ComponentModel.DataAnnotations;

namespace Vitarish.Users.Domain.Entities;

/// <summary>
/// Represents a WebAuthn/FIDO2 credential (passkey) associated with a user.
/// </summary>
public class WebAuthnCredential
{
    /// <summary>
    /// The unique identifier for this credential record.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The user ID this credential belongs to.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    /// <summary>
    /// Navigation property to the User entity.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// The credential ID (raw bytes from the authenticator).
    /// </summary>
    [Required]
    public required byte[] CredentialId { get; set; }

    /// <summary>
    /// The public key for this credential in COSE format.
    /// </summary>
    [Required]
    public required byte[] PublicKey { get; set; }

    /// <summary>
    /// The signature counter value from the authenticator.
    /// Used to detect cloned credentials.
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>
    /// The credential type (e.g., "public-key").
    /// </summary>
    [Required]
    [MaxLength(32)]
    public required string CredentialType { get; set; }

    /// <summary>
    /// The Authenticator Attestation GUID (AAGUID).
    /// Identifies the authenticator model.
    /// </summary>
    public Guid? AaGuid { get; set; }

    /// <summary>
    /// Optional friendly name for this credential (e.g., "YubiKey", "Touch ID").
    /// </summary>
    [MaxLength(200)]
    public string? FriendlyName { get; set; }

    /// <summary>
    /// When this credential was registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// When this credential was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// The transport methods supported by this credential (e.g., "usb", "nfc", "ble", "internal").
    /// Stored as a comma-separated list.
    /// </summary>
    [MaxLength(100)]
    public string? Transports { get; set; }

    /// <summary>
    /// Whether this credential is currently active and can be used for authentication.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
