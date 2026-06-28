using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Infrastructure;

public sealed class WebAuthnAuthenticationService(
    IFido2 fido2,
    IDistributedCache cache,
    IWebAuthnCredentialLookup credentialLookup) : IWebAuthnAuthenticationService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public async Task<WebAuthnBeginResult> BeginAsync(string? userNameOrEmail, CancellationToken cancellationToken)
    {
        string challengeId = Guid.NewGuid().ToString("N");

        List<PublicKeyCredentialDescriptor> allowedCredentials = [];
        if (!string.IsNullOrWhiteSpace(userNameOrEmail))
        {
            IReadOnlyList<WebAuthnCredentialDescriptor> credentials = await credentialLookup.GetAllowedCredentialsAsync(userNameOrEmail, cancellationToken);
            allowedCredentials = credentials.Select(c => new PublicKeyCredentialDescriptor(c.CredentialId)).ToList();
        }

        AssertionOptions options = fido2.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials,
                UserVerification = UserVerificationRequirement.Preferred
            }) ?? new AssertionOptions
            {
                AllowCredentials = allowedCredentials,
                UserVerification = UserVerificationRequirement.Preferred
            };

        string optionsJson = options.ToJson();
        await CacheAssertionOptionsAsync(challengeId, optionsJson, cancellationToken);

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime);
        return new WebAuthnBeginResult(challengeId, optionsJson, expiresAt, 5);
    }

    public async Task<WebAuthnCompleteResult> CompleteAsync(string challengeId, string credentialJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(credentialJson))
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.InvalidInput, "Challenge ID and credential JSON are required."));

        string? cachedOptions = await LoadAssertionOptionsAsync(challengeId, cancellationToken);
        if (cachedOptions is null)
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.ChallengeExpired, "Passkey challenge has expired. Please start again."));

        AssertionOptions? options = AssertionOptions.FromJson(cachedOptions);
        if (options is null)
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.ProviderFailure, "Failed to parse passkey challenge options."));

        AuthenticatorAssertionRawResponse? assertionResponse;
        try
        {
            assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(credentialJson);
        }
        catch
        {
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.InvalidInput, "Invalid credential response format."));
        }

        if (assertionResponse is null)
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.InvalidInput, "Invalid credential response."));

        WebAuthnCredentialData? storedCredential = await credentialLookup.FindByCredentialIdAsync(assertionResponse.RawId, cancellationToken);
        if (storedCredential is null || !storedCredential.IsActive)
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.UnknownCredential, "Passkey credential not found."));

        VerifyAssertionResult result;
        try
        {
            result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                IsUserHandleOwnerOfCredentialIdCallback = async (_, _) => true,
                OriginalOptions = options,
                StoredPublicKey = storedCredential.PublicKey,
                StoredSignatureCounter = storedCredential.SignatureCounter
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.InvalidCredential, $"Passkey verification failed: {ex.Message}"));
        }

        if (result.SignCount > 0 && result.SignCount <= storedCredential.SignatureCounter)
        {
            return new WebAuthnCompleteResult(false, null, new Application.Results.AuthenticationFailure(Domain.Security.AuthenticationFailureReason.InvalidCredential, "Potential cloned authenticator detected."));
        }

        await credentialLookup.UpdateSignatureCounterAsync(assertionResponse.RawId, result.SignCount, cancellationToken);
        await RemoveAssertionOptionsAsync(challengeId, cancellationToken);

        return new WebAuthnCompleteResult(true, storedCredential.UserId, null);
    }

    private async Task CacheAssertionOptionsAsync(string challengeId, string optionsJson, CancellationToken ct)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(optionsJson);
        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = ChallengeLifetime
        };
        await cache.SetAsync(GetCacheKey(challengeId), bytes, entryOptions, ct);
    }

    private async Task<string?> LoadAssertionOptionsAsync(string challengeId, CancellationToken ct)
    {
        byte[]? bytes = await cache.GetAsync(GetCacheKey(challengeId), ct);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    private async Task RemoveAssertionOptionsAsync(string challengeId, CancellationToken ct)
    {
        await cache.RemoveAsync(GetCacheKey(challengeId), ct);
    }

    private static string GetCacheKey(string challengeId) => $"authentication:passkey:assertion:{challengeId}";
}
