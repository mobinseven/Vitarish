using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class BeginPasskeyLoginHandlerTests
{
    [Fact]
    public async Task Challenge_reports_the_configured_failed_attempt_threshold()
    {
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.BeginAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnBeginResult("challenge-1", "{}", DateTimeOffset.UtcNow.AddMinutes(5), 5));
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with { FailedAttemptThreshold = 7 };
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        BeginPasskeyLoginHandler handler = new(
            webAuthn.Object,
            Mock.Of<IAuthenticationAttemptTracker>(),
            policyProvider.Object,
            new AuthenticationAttemptKeyFactory(Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "tests",
                CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
            })));

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginPasskeyLoginCommand(null, null),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>()
            .Which.Value.RemainingAttempts.Should().Be(7);
    }
}
