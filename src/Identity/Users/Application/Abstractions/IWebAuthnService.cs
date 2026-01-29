using Fido2NetLib.Objects;

namespace Vitarish.Users.Application.Abstractions;

/// <summary>
/// Contract for WebAuthn/Passkey authentication operations.
/// </summary>
public interface IWebAuthnService
{
    /// <summary>
    /// Begins the WebAuthn registration ceremony for the authenticated user.
    /// </summary>
    /// <param name="userId">The ID of the user registering a credential.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Registration options for the client to create a credential.</returns>
    Task<CredentialCreateOptions> BeginRegistrationAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Completes the WebAuthn registration ceremony and stores the credential.
    /// </summary>
    /// <param name="userId">The ID of the user registering a credential.</param>
    /// <param name="attestationResponse">The attestation response from the authenticator.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The credential ID if successful.</returns>
    Task<string> CompleteRegistrationAsync(string userId, AuthenticatorAttestationRawResponse attestationResponse, CancellationToken ct = default);

    /// <summary>
    /// Begins the WebAuthn authentication ceremony for a user.
    /// </summary>
    /// <param name="username">The username to authenticate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Authentication options for the client.</returns>
    Task<AssertionOptions> BeginAuthenticationAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Completes the WebAuthn authentication ceremony and signs in the user.
    /// </summary>
    /// <param name="assertionResponse">The assertion response from the authenticator.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if authentication was successful.</returns>
    Task<bool> CompleteAuthenticationAsync(AuthenticatorAssertionRawResponse assertionResponse, CancellationToken ct = default);

    /// <summary>
    /// Lists all active WebAuthn credentials for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of credential DTOs.</returns>
    Task<IEnumerable<WebAuthnCredentialDto>> ListCredentialsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a WebAuthn credential.
    /// </summary>
    /// <param name="credentialId">The ID of the credential to delete.</param>
    /// <param name="userId">The ID of the user owning the credential.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the credential was deleted.</returns>
    Task<bool> DeleteCredentialAsync(Guid credentialId, string userId, CancellationToken ct = default);
}

/// <summary>
/// DTO for WebAuthn credential information.
/// </summary>
public record WebAuthnCredentialDto
{
    public Guid Id { get; init; }
    public string? FriendlyName { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? Transports { get; init; }
}
