using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            new TestAuthenticationUtcClock(),
            NullLogger<AuthenticationOtpDeliveryService>.Instance);
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

    [Fact]
    public async Task A_delivery_failure_is_logged_with_the_provider_error_and_masked_recipient_then_rethrown()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        RecordingLogger logger = new();
        AuthenticationOtpDeliveryService delivery = new(writer.Object, requestContext.Object, new TestAuthenticationUtcClock(), logger);
        InvalidOperationException providerFailure = new("smtp handshake failed");

        Func<Task> send = () => delivery.SendEmailCodeAsync("owner@example.test", () => throw providerFailure);

        (await send.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(providerFailure);
        RecordingLogger.Entry entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(providerFailure);
        entry.Message.Should().Contain(AuthenticationSecurityAuditEvents.OtpEmailSendOperation).And.Contain("***er@***");
        entry.Message.Should().NotContain("owner@example.test");
    }

    private sealed class RecordingLogger : ILogger<AuthenticationOtpDeliveryService>
    {
        public sealed record Entry(LogLevel Level, string Message, Exception? Exception);

        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new(logLevel, formatter(state, exception), exception));
    }

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc);
    }
}
