namespace Vitarish.Authentication.Application.Abstractions;

public interface IWebAuthnCredentialLookup
{
    Task<WebAuthnCredentialData?> FindByCredentialIdAsync(byte[] credentialId, CancellationToken ct);
    Task UpdateSignatureCounterAsync(byte[] credentialId, uint newCounter, CancellationToken ct);
}

public record WebAuthnCredentialData(string UserId, byte[] PublicKey, uint SignatureCounter, bool IsActive);
