using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Security;

public sealed class AuthenticationSourceAttemptGuardTests
{
    private static readonly SecurityAccessPolicy Policy = SecurityAccessPolicy.Default with { SourceFailedAttemptThreshold = 3 };

    [Fact]
    public async Task An_address_is_denied_once_its_failures_reach_the_threshold_and_the_block_is_audited()
    {
        Fixture fixture = new();
        AuthenticationSourceAttemptGuard guard = fixture.CreateGuard("198.51.100.10");

        bool[] blocked =
        [
            await guard.RegisterFailureAsync(Policy, CancellationToken.None),
            await guard.RegisterFailureAsync(Policy, CancellationToken.None),
            await guard.RegisterFailureAsync(Policy, CancellationToken.None)
        ];

        blocked.Should().Equal(false, false, true);
        (await guard.IsBlockedAsync(CancellationToken.None)).Should().BeTrue();
        SecurityAuditEvent audited = fixture.Events.Should().ContainSingle().Subject;
        audited.EventName.Should().Be(SecurityAuditEventCatalog.AuthenticationSourceBlockedEvent);
        audited.Operation.Should().Be(SecurityAuditEventCatalog.AuthenticationSourceBlockOperation);
        audited.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        audited.RedactedReason.Should().Be(SecurityAuditReasonCatalog.SourceFailedAttemptThreshold);
    }

    [Fact]
    public async Task Failures_from_one_address_do_not_deny_another()
    {
        Fixture fixture = new();
        AuthenticationSourceAttemptGuard attacker = fixture.CreateGuard("198.51.100.10");
        for (int attempt = 0; attempt < Policy.SourceFailedAttemptThreshold; attempt++)
            _ = await attacker.RegisterFailureAsync(Policy, CancellationToken.None);

        (await fixture.CreateGuard("198.51.100.11").IsBlockedAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task A_request_without_a_client_address_is_never_counted_or_denied()
    {
        Fixture fixture = new();
        AuthenticationSourceAttemptGuard guard = fixture.CreateGuard(null);

        for (int attempt = 0; attempt < Policy.SourceFailedAttemptThreshold + 1; attempt++)
            (await guard.RegisterFailureAsync(Policy, CancellationToken.None)).Should().BeFalse();

        (await guard.IsBlockedAsync(CancellationToken.None)).Should().BeFalse();
        fixture.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task A_failed_audit_write_does_not_lift_the_denial()
    {
        Fixture fixture = new(auditFails: true);
        AuthenticationSourceAttemptGuard guard = fixture.CreateGuard("198.51.100.10");

        for (int attempt = 0; attempt < Policy.SourceFailedAttemptThreshold - 1; attempt++)
            _ = await guard.RegisterFailureAsync(Policy, CancellationToken.None);

        (await guard.RegisterFailureAsync(Policy, CancellationToken.None)).Should().BeTrue();
        (await guard.IsBlockedAsync(CancellationToken.None)).Should().BeTrue();
    }

    private sealed class Fixture(bool auditFails = false)
    {
        private readonly InMemoryAuthenticationAttemptTracker tracker = new();
        private readonly AuthenticationAttemptKeyFactory keys = new(Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

        public List<SecurityAuditEvent> Events { get; } = [];

        public AuthenticationSourceAttemptGuard CreateGuard(string? clientIpAddress)
        {
            Mock<IClientIpAddressAccessor> clientIp = new();
            clientIp.SetupGet(item => item.ClientIpAddress).Returns(clientIpAddress);
            Mock<IRequestContext> requestContext = new();
            requestContext.SetupGet(item => item.TenantId).Returns("tenant-a");
            Mock<ISecurityAuditWriter> writer = new();
            if (auditFails)
                writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new SecurityAuditStoreException("unavailable", new InvalidOperationException()));
            else
                writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
                    .Callback((SecurityAuditEvent auditEvent, CancellationToken _) => Events.Add(auditEvent))
                    .ReturnsAsync(SecurityAuditWriteResult.Recorded);
            return new AuthenticationSourceAttemptGuard(tracker, keys, clientIp.Object, writer.Object, requestContext.Object);
        }
    }
}
