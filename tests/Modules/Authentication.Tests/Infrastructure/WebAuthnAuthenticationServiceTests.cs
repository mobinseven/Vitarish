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
        credentialLookup
            .Setup(item => item.GetAllowedCredentialsAsync("login-text@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WebAuthnCredentialDescriptor>());

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

    private static IAuthenticationSecurityPolicyProvider PolicyProvider()
    {
        Mock<IAuthenticationSecurityPolicyProvider> provider = new();
        provider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        return provider.Object;
    }
}
