namespace Vitarish.Authentication.Application.Abstractions;

public interface IWebAuthnCredentialLookup
{
    Task<IReadOnlyList<WebAuthnCredentialDescriptor>> GetAllowedCredentialsAsync(string userId, CancellationToken ct);
    Task<WebAuthnCredentialData?> FindByCredentialIdAsync(byte[] credentialId, CancellationToken ct);
    Task UpdateSignatureCounterAsync(byte[] credentialId, uint newCounter, CancellationToken ct);
}

public record WebAuthnCredentialDescriptor(byte[] CredentialId);

public record WebAuthnCredentialData(string UserId, byte[] PublicKey, uint SignatureCounter, bool IsActive);
