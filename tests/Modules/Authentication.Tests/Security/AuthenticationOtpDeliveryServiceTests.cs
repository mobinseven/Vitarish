using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Security;

public sealed class AuthenticationOtpDeliveryServiceTests
{
    [Fact]
    public async Task Sends_and_audits_pre_authentication_delivery_without_tenant_context()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns((string?)null);
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        AuthenticationOtpDeliveryService delivery = new(
            writer.Object,
            requestContext.Object,
            new TestAuthenticationUtcClock());
        bool sent = false;

        await delivery.SendSmsCodeAsync("+12125550123", () =>
        {
            sent = true;
            return Task.CompletedTask;
        });

        sent.Should().BeTrue();
        SecurityAuditEvent auditEvent = writer.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Single();
        auditEvent.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        auditEvent.Resource.Should().Be(AuthenticationSecurityAuditEvents.OtpDeliveryResourcePrefix + "*******0123");
        auditEvent.Resource.Should().NotContain("+12125550123");
    }

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc);
    }
}
