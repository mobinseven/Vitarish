using FluentAssertions;
using Vitarish.Authentication.Application.Results;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Infrastructure;
using System.Security.Claims;
using Vitarish.Requests.Application.Abstractions;

namespace Authentication.Tests.Handlers;

public sealed class CentralAuthenticationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Personal_session_revocation_refuses_foreign_browser_sessions()
    {
        Mock<IAuthenticationBrowserSessionDirectory> directory = new();
        directory.Setup(item => item.OwnsAsync("owner", "foreign-browser", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IAuthenticationSessionRegistry> registry = new();
        Mock<IRequestContext> request = new();
        request.SetupGet(item => item.UserId).Returns("owner");
        request.SetupGet(item => item.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthenticationSessionClaimTypes.SessionId, "current-browser")], "cookie")));
        PersonalBrowserSessionHandler handler = new(directory.Object, registry.Object, request.Object);
        (await handler.RevokeAsync("foreign-browser", CancellationToken.None)).Should().BeFalse();
        registry.Verify(item => item.RevokeAsync(It.IsAny<string>(), It.IsAny<AuthenticationSessionTerminationReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/continue?request=opaque", true)]
    [InlineData("/account/security", true)]
    [InlineData("/patients", false)]
    [InlineData("/account-other", false)]
    [InlineData("//external.example/account", false)]
    public void Central_destinations_do_not_capture_clinic_pages_or_external_origins(string path, bool expected) =>
        new AuthenticationCentralDestinationPolicy().Owns(path).Should().Be(expected);

    [Fact]
    public async Task Pending_challenge_read_preserves_expiry_attempts_and_server_destination()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailLoginChallenge("user", "owner@example.test", Now);
        InMemoryAuthenticationChallengeStore store = new();
        await store.SaveAsync(challenge, CancellationToken.None);
        Mock<IPendingAuthenticationChallenge> pending = new();
        pending.Setup(item => item.Read()).Returns(new PendingChallengeReference(new ChallengeResult(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts), "/continue"));
        Mock<IAuthenticationUtcClock> clock = new();
        clock.SetupGet(item => item.UtcNow).Returns(Now.UtcDateTime);
        GetPendingAuthenticationChallengeHandler handler = new(pending.Object, store, clock.Object);

        var result = await handler.Handle(CancellationToken.None);

        result.Should().NotBeNull();
        result!.Destination.Should().Be(challenge.Destination);
        result.Challenge.ExpiresAt.Should().Be(challenge.ExpiresAt);
        result.Challenge.RemainingAttempts.Should().Be(challenge.RemainingAttempts);
        challenge.Status.Should().Be(AuthenticationChallengeStatus.Pending);
        pending.Verify(item => item.Clear(), Times.Never);
        clock.SetupGet(item => item.UtcNow).Returns(challenge.ExpiresAt.UtcDateTime);
        (await handler.Handle(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Consumed_or_exhausted_challenge_is_not_resumed()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailLoginChallenge("user", "owner@example.test", Now);
        Mock<IAuthenticationChallengeStore> store = new();
        store.Setup(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        Mock<IPendingAuthenticationChallenge> pending = new();
        pending.Setup(item => item.Read()).Returns(new PendingChallengeReference(new ChallengeResult(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts), null));
        Mock<IAuthenticationUtcClock> clock = new();
        clock.SetupGet(item => item.UtcNow).Returns(Now.UtcDateTime);
        GetPendingAuthenticationChallengeHandler handler = new(pending.Object, store.Object, clock.Object);
        for (int attempt = 0; attempt < challenge.MaximumAttempts; attempt++) challenge.RegisterFailure(Now);
        (await handler.Handle(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public void Resume_cookie_is_protected_host_only_and_unreadable_after_tampering()
    {
        DefaultHttpContext context = new();
        context.Request.Scheme = Uri.UriSchemeHttps;
        HttpContextAccessor accessor = new() { HttpContext = context };
        PendingAuthenticationChallengeCookie cookie = new(accessor, new EphemeralDataProtectionProvider());
        cookie.Store(new ChallengeResult("challenge-reference", AuthenticationChallengePurpose.EmailLogin, Now.AddMinutes(5), 5), "/continue");
        string header = context.Response.Headers.SetCookie.ToString();
        header.Should().Contain("httponly").And.Contain("secure").And.NotContain("domain=").And.NotContain("challenge-reference");
        context.Request.Headers.Cookie = header.Split(';')[0];
        cookie.Read()!.Challenge.ChallengeId.Should().Be("challenge-reference");
        context.Request.Headers.Cookie = context.Request.Headers.Cookie.ToString().Split('=')[0] + "=tampered";
        cookie.Read().Should().BeNull();
    }

    [Theory]
    [InlineData(AuthenticationChallengePurpose.EmailPasswordRecovery)]
    [InlineData(AuthenticationChallengePurpose.PhonePasswordRecovery)]
    public async Task Recovery_resume_never_reads_account_eligibility_delivery_or_challenge_status(AuthenticationChallengePurpose purpose)
    {
        Mock<IAuthenticationChallengeStore> store = new(MockBehavior.Strict);
        Mock<IPendingAuthenticationChallenge> pending = new();
        ChallengeResult publicChallenge = new("opaque-public-reference", purpose, Now.AddMinutes(5), 5);
        pending.Setup(item => item.Read()).Returns(new PendingChallengeReference(publicChallenge, null));
        Mock<IAuthenticationUtcClock> clock = new();
        clock.SetupGet(item => item.UtcNow).Returns(Now.UtcDateTime);
        var result = await new GetPendingAuthenticationChallengeHandler(pending.Object, store.Object, clock.Object).Handle(CancellationToken.None);
        result.Should().Be(new PendingAuthenticationChallenge(publicChallenge, string.Empty, null));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Password_second_factor_resume_never_discloses_the_phone_destination()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge("subject", "+989123456789", Now, requiresProfileCompletion: false);
        Mock<IAuthenticationChallengeStore> store = new();
        store.Setup(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        Mock<IPendingAuthenticationChallenge> pending = new();
        pending.Setup(item => item.Read()).Returns(new PendingChallengeReference(new ChallengeResult(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, challenge.RemainingAttempts), null));
        Mock<IAuthenticationUtcClock> clock = new();
        clock.SetupGet(item => item.UtcNow).Returns(Now.UtcDateTime);
        var result = await new GetPendingAuthenticationChallengeHandler(pending.Object, store.Object, clock.Object).Handle(CancellationToken.None);
        result!.Destination.Should().BeEmpty();
    }

    [Fact]
    public void Concurrent_continuations_keep_independent_browser_correlations()
    {
        DefaultHttpContext context = new();
        HttpContextAccessor accessor = new() { HttpContext = context };
        AuthenticationBrowserCorrelation cookies = new(accessor);
        string first = TenantHandoffTokens.Create();
        string second = TenantHandoffTokens.Create();
        cookies.Write(first, Now.UtcDateTime.AddMinutes(10));
        cookies.Write(second, Now.UtcDateTime.AddMinutes(10));
        context.Request.Headers.Cookie = string.Join("; ", context.Response.Headers.SetCookie.Select(header => header!.Split(';')[0]));
        cookies.Read(TenantHandoffTokens.Hash(first)).Should().Be(first);
        cookies.Read(TenantHandoffTokens.Hash(second)).Should().Be(second);
    }

    [Fact]
    public void Correlation_cookie_jar_is_bounded_and_redemption_removes_its_reference()
    {
        Dictionary<string, string> jar = new();
        string latest = string.Empty;
        for (int index = 0; index < AuthenticationContinuation.MaximumBrowserContinuations + 3; index++)
        {
            DefaultHttpContext context = new();
            context.Request.Headers.Cookie = string.Join("; ", jar.Select(item => item.Key + "=" + item.Value));
            AuthenticationBrowserCorrelation cookies = new(new HttpContextAccessor { HttpContext = context });
            latest = TenantHandoffTokens.Create();
            cookies.Write(latest, Now.UtcDateTime.AddMinutes(10).AddSeconds(index));
            foreach (string? header in context.Response.Headers.SetCookie)
            {
                string[] pair = header!.Split(';')[0].Split('=', 2);
                if (pair[1].Length == 0) jar.Remove(pair[0]); else jar[pair[0]] = pair[1];
            }
        }
        jar.Count.Should().Be(AuthenticationContinuation.MaximumBrowserContinuations);
        DefaultHttpContext redemption = new();
        redemption.Request.Headers.Cookie = string.Join("; ", jar.Select(item => item.Key + "=" + item.Value));
        AuthenticationBrowserCorrelation reader = new(new HttpContextAccessor { HttpContext = redemption });
        reader.Read(TenantHandoffTokens.Hash(latest)).Should().Be(latest);
        reader.Clear(TenantHandoffTokens.Hash(latest));
        redemption.Response.Headers.SetCookie.Should().ContainSingle();
        redemption.Response.Headers.SetCookie.ToString().Should().Contain("expires=");
    }

    [Fact]
    public async Task Continuation_completion_is_single_use_across_competing_contexts()
    {
        var options = new DbContextOptionsBuilder<AuthenticationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using AuthenticationDbContext firstDatabase = new(options);
        await using AuthenticationDbContext secondDatabase = new(options);
        AuthenticationContinuationStore first = new(firstDatabase);
        AuthenticationContinuationStore second = new(secondDatabase);
        await first.AddAsync(new AuthenticationContinuation
        {
            TokenHash = "opaque-request-hash", Origin = "https://app.example.test", ReturnPath = "/home",
            CorrelationHash = "browser-correlation-hash", ExpiresAt = Now.UtcDateTime.AddMinutes(10)
        }, CancellationToken.None);
        var firstRead = await first.FindAsync("opaque-request-hash", CancellationToken.None);
        var secondRead = await second.FindAsync("opaque-request-hash", CancellationToken.None);
        (await first.TryCompleteAsync(firstRead!, Now.UtcDateTime, CancellationToken.None)).Should().BeTrue();
        (await second.TryCompleteAsync(secondRead!, Now.UtcDateTime, CancellationToken.None)).Should().BeFalse();
    }
}
