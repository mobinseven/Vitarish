using FluentAssertions;
using Fido2NetLib;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class WebAuthnAuthenticationServiceTests
{
    [Fact]
    public async Task Begin_caches_only_the_opaque_attempt_key_for_the_login_identifier()
    {
        Mock<IFido2> fido2 = new();
        Mock<IWebAuthnCredentialLookup> credentialLookup = new();

        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        AuthenticationAttemptKeyFactory attemptKeyFactory = new(
            Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "realm-a",
                CurrentPepper = "current-authentication-attempt-pepper-20260829"
            }));
        WebAuthnAuthenticationService service = new(fido2.Object, cache, credentialLookup.Object, attemptKeyFactory, PolicyProvider());

        WebAuthnBeginResult result = await service.BeginAsync("login-text@example.test", CancellationToken.None);

        string? cachedAttemptKey = await cache.GetStringAsync($"authentication:passkey:attempt:{result.ChallengeId}");
        cachedAttemptKey.Should().Be(attemptKeyFactory.Identifier(AuthenticationMethod.Passkey, "login-text@example.test"));
        cachedAttemptKey!.ToUpperInvariant().Should().NotContain("LOGIN-TEXT");
        cachedAttemptKey.ToUpperInvariant().Should().NotContain("EXAMPLE.TEST");
    }

    [Fact]
    public async Task Named_begin_lists_no_keys_and_never_looks_up_the_typed_name()
    {
        Mock<IWebAuthnCredentialLookup> credentialLookup = new(MockBehavior.Strict);
        WebAuthnAuthenticationService service = new(Mock.Of<IFido2>(), new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), credentialLookup.Object, AttemptKeyFactory(), PolicyProvider());

        WebAuthnBeginResult existing = await service.BeginAsync("owner@example.test", CancellationToken.None);
        WebAuthnBeginResult unknown = await service.BeginAsync("nobody@example.test", CancellationToken.None);

        // The same answer for every name, so sign-in cannot reveal which accounts exist.
        AllowedCredentialCount(existing.OptionsJson).Should().Be(0);
        AllowedCredentialCount(unknown.OptionsJson).Should().Be(0);
        WithoutChallenge(existing.OptionsJson).Should().Be(WithoutChallenge(unknown.OptionsJson));
    }

    [Fact]
    public async Task Completion_subject_is_the_owner_of_the_presented_credential()
    {
        byte[] credentialId = [7, 8, 9];
        Mock<IWebAuthnCredentialLookup> credentialLookup = new();
        credentialLookup
            .Setup(item => item.FindByCredentialIdAsync(It.Is<byte[]>(id => id.SequenceEqual(credentialId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCredentialData("user-7", [1], 0, true));
        WebAuthnAuthenticationService service = new(Mock.Of<IFido2>(), new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), credentialLookup.Object, AttemptKeyFactory(), PolicyProvider());
        WebAuthnBeginResult result = await service.BeginAsync("someone-else@example.test", CancellationToken.None);
        string credentialJson = JsonSerializer.Serialize(new { rawId = Convert.ToBase64String(credentialId) });

        (await service.GetSubjectIdAsync(result.ChallengeId, credentialJson, CancellationToken.None)).Should().Be("user-7");
    }

    [Fact]
    public async Task Discoverable_begin_does_not_create_a_shared_account_attempt_key()
    {
        Mock<IFido2> fido2 = new();
        Mock<IWebAuthnCredentialLookup> credentialLookup = new();
        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        AuthenticationAttemptKeyFactory attemptKeyFactory = new(
            Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "realm-a",
                CurrentPepper = "current-authentication-attempt-pepper-20260829"
            }));
        WebAuthnAuthenticationService service = new(fido2.Object, cache, credentialLookup.Object, attemptKeyFactory, PolicyProvider());

        WebAuthnBeginResult first = await service.BeginAsync(null, CancellationToken.None);
        WebAuthnBeginResult second = await service.BeginAsync(null, CancellationToken.None);

        (await service.GetAttemptKeyAsync(first.ChallengeId, CancellationToken.None)).Should().BeNull();
        (await service.GetAttemptKeyAsync(second.ChallengeId, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Discoverable_completion_attempt_key_is_derived_from_the_credential_id()
    {
        Mock<IFido2> fido2 = new();
        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        AuthenticationAttemptKeyFactory attemptKeyFactory = new(
            Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "realm-a",
                CurrentPepper = "current-authentication-attempt-pepper-20260829"
            }));
        WebAuthnAuthenticationService service = new(fido2.Object, cache, Mock.Of<IWebAuthnCredentialLookup>(), attemptKeyFactory, PolicyProvider());
        WebAuthnBeginResult result = await service.BeginAsync(null, CancellationToken.None);
        string credentialJson = JsonSerializer.Serialize(new { rawId = Convert.ToBase64String([1, 2, 3]) });

        string? key = await service.GetAttemptKeyAsync(result.ChallengeId, credentialJson, CancellationToken.None);

        key.Should().Be(attemptKeyFactory.Identifier(AuthenticationMethod.Passkey, Convert.ToBase64String([1, 2, 3])));
    }

    [Fact]
    public async Task Malformed_discoverable_completion_uses_a_challenge_scoped_attempt_key()
    {
        Mock<IFido2> fido2 = new();
        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        AuthenticationAttemptKeyFactory attemptKeyFactory = new(
            Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "realm-a",
                CurrentPepper = "current-authentication-attempt-pepper-20260829"
            }));
        WebAuthnAuthenticationService service = new(fido2.Object, cache, Mock.Of<IWebAuthnCredentialLookup>(), attemptKeyFactory, PolicyProvider());

        string? key = await service.GetAttemptKeyAsync("challenge-42", "not-json", CancellationToken.None);

        key.Should().Be(attemptKeyFactory.Identifier(AuthenticationMethod.Passkey, "challenge:challenge-42"));
    }

    private static AuthenticationAttemptKeyFactory AttemptKeyFactory() => new(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "realm-a",
            CurrentPepper = "current-authentication-attempt-pepper-20260829"
        }));

    private static int AllowedCredentialCount(string optionsJson) =>
        JsonDocument.Parse(optionsJson).RootElement.TryGetProperty("allowCredentials", out JsonElement list) ? list.GetArrayLength() : 0;

    private static string WithoutChallenge(string optionsJson)
    {
        Dictionary<string, JsonElement> options = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(optionsJson)!;
        options.Remove("challenge");
        return JsonSerializer.Serialize(options);
    }

    private static IAuthenticationSecurityPolicyProvider PolicyProvider()
    {
        Mock<IAuthenticationSecurityPolicyProvider> provider = new();
        provider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        return provider.Object;
    }
}
