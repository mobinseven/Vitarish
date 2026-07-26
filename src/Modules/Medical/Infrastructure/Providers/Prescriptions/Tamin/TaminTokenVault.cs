using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Vitarish.Medical.Domain.Entities.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminTokenVault(IDataProtectionProvider protectionProvider)
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("Vitarish.Medical.TaminTokens.v1");

    public TaminStoredToken Read(DoctorProviderCredential credential) => new(
        Unprotect(credential.TokenSecretRef),
        Unprotect(credential.RefreshTokenSecretRef),
        credential.TokenExpiresAt is { } expiresAt
            ? new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))
            : null);

    public void Write(DoctorProviderCredential credential, string accessToken, string? refreshToken, DateTimeOffset? expiresAt)
    {
        credential.TokenSecretRef = _protector.Protect(accessToken);
        credential.RefreshTokenSecretRef = string.IsNullOrWhiteSpace(refreshToken)
            ? credential.RefreshTokenSecretRef
            : _protector.Protect(refreshToken);
        credential.TokenExpiresAt = expiresAt?.UtcDateTime;
    }

    public void Clear(DoctorProviderCredential credential)
    {
        credential.TokenSecretRef = null;
        credential.RefreshTokenSecretRef = null;
        credential.TokenExpiresAt = null;
    }

    public void WriteAuthorizationState(
        DoctorProviderCredential credential,
        string state,
        string codeVerifier,
        DateTimeOffset expiresAt)
    {
        string payload = JsonSerializer.Serialize(new TaminAuthorizationState(state, codeVerifier, expiresAt));
        credential.SessionId = _protector.Protect(payload);
    }

    public TaminAuthorizationState? ReadAuthorizationState(DoctorProviderCredential credential)
    {
        if (string.IsNullOrWhiteSpace(credential.SessionId))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TaminAuthorizationState>(_protector.Unprotect(credential.SessionId));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return null;
        }
    }

    private string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(value);
        }
        catch (CryptographicException)
        {
            // Compatibility window for tokens written by the v2 adapter before at-rest protection.
            return value;
        }
    }
}

internal sealed record TaminStoredToken(string? AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt);
internal sealed record TaminAuthorizationState(string State, string CodeVerifier, DateTimeOffset ExpiresAt);
