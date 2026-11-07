using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class SmsAuthenticationCodeSenderTests
{
    [Fact]
    public async Task Password_phone_code_is_not_logged_when_diagnostic_code_logging_is_enabled()
    {
        Mock<IMessagesService> messagesService = new();
        messagesService
            .Setup(service => service.SendSmsCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns("test-tenant");
        requestContext.SetupGet(context => context.RequestId).Returns("sender-test-request");
        RecordingLogger<AuthenticationOtpDeliveryService> deliveryLogger = new();
        AuthenticationOtpDeliveryService delivery = new(
            auditWriter.Object,
            requestContext.Object,
            new TestAuthenticationUtcClock(),
            deliveryLogger);
        RecordingLogger<SmsAuthenticationCodeSender> senderLogger = new();
        SmsAuthenticationCodeSender sender = new(
            messagesService.Object,
            new AllowSmsRequestShield(),
            delivery,
            Options.Create(new DeliveredCodeLogOptions { Enabled = true }),
            senderLogger);

        await sender.SendAsync(
            "+989130000001",
            "936251",
            AuthenticationChallengePurpose.PasswordLoginOtp,
            CancellationToken.None);

        senderLogger.Entries.Should().BeEmpty();
        deliveryLogger.Entries.Should().BeEmpty();
        auditWriter.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Should().ContainSingle(audit => audit.Resource != null && audit.Resource.Contains("*******0001", StringComparison.Ordinal))
            .Which.Resource.Should().NotContain("+989130000001").And.NotContain("936251");
    }

    private sealed class AllowSmsRequestShield : VerificationSmsRequestShield
    {
        public override bool CanSend(string phoneNumber) => true;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
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
        public DateTime UtcNow => new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
    }
}
