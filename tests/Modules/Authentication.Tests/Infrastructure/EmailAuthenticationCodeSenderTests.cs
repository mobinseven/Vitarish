using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Email;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class EmailAuthenticationCodeSenderTests
{
    [Theory]
    [InlineData("en", "Your verification code")]
    [InlineData("fa", "کد تأیید شما")]
    public async Task Password_email_code_uses_the_localized_template_without_logging_the_code(
        string cultureName,
        string expectedText)
    {
        using CultureScope culture = new(cultureName);
        using SenderFixture fixture = new(deliveredCodeLoggingEnabled: true);

        await fixture.Sender.SendAsync(
            "owner@example.test",
            "936251",
            AuthenticationChallengePurpose.PasswordLoginEmailOtp,
            CancellationToken.None);

        fixture.SentMessage.Should().NotBeNull();
        fixture.SentMessage!.Subject.Should().Be(expectedText);
        fixture.SentMessage.Body.Should().Contain(expectedText).And.Contain("936251");
        fixture.SentMessage.HtmlBody.Should().Contain(expectedText).And.Contain("<strong>936251</strong>");
        fixture.SenderLogger.Entries.Should().BeEmpty();
        fixture.AuditWriter.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Should().ContainSingle(audit => audit.Resource != null && audit.Resource.Contains("***er@***", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Passwordless_email_code_keeps_the_legacy_english_template_under_persian_culture()
    {
        using CultureScope culture = new("fa");
        using SenderFixture fixture = new(deliveredCodeLoggingEnabled: false);

        await fixture.Sender.SendAsync(
            "owner@example.test",
            "936251",
            AuthenticationChallengePurpose.EmailLogin,
            CancellationToken.None);

        fixture.SentMessage.Should().NotBeNull();
        fixture.SentMessage!.Subject.Should().Be("Your verification code");
        fixture.SentMessage.Body.Should().Be("936251");
        fixture.SentMessage.HtmlBody.Should().Be("<p>Your verification code is: <strong>936251</strong></p>");
        fixture.SenderLogger.Entries.Should().BeEmpty();
        fixture.AuditWriter.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Should().ContainSingle(audit => audit.Resource != null && audit.Resource.Contains("***er@***", StringComparison.Ordinal))
            .Which.Resource.Should().NotContain("936251");
    }

    [Fact]
    public async Task Provider_exception_content_that_contains_the_code_is_not_logged()
    {
        using CultureScope culture = new("en");
        using SenderFixture fixture = new(deliveredCodeLoggingEnabled: true);
        InvalidOperationException providerFailure = new("SMTP provider echoed code 936251 in its response.");
        fixture.MessagesService
            .Setup(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(providerFailure);

        Func<Task> send = () => fixture.Sender.SendAsync(
            "owner@example.test",
            "936251",
            AuthenticationChallengePurpose.PasswordLoginEmailOtp,
            CancellationToken.None);

        (await send.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(providerFailure);
        fixture.DeliveryLogger.Entries.Should().ContainSingle();
        RecordingLogger<AuthenticationOtpDeliveryService>.Entry deliveryLog = fixture.DeliveryLogger.Entries.Single();
        deliveryLog.Exception.Should().BeNull();
        deliveryLog.Message.Should().Contain(nameof(InvalidOperationException));
        deliveryLog.Message.Should().NotContain("936251").And.NotContain(providerFailure.Message);
        fixture.SenderLogger.Entries.Should().BeEmpty();
        fixture.AuditWriter.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Should().ContainSingle(audit => audit.RedactedReason == AuthenticationSecurityAuditEvents.OtpDeliveryFailure)
            .Which.RedactedReason.Should().NotContain("936251");
    }

    private sealed class SenderFixture : IDisposable
    {
        public SenderFixture(bool deliveredCodeLoggingEnabled)
        {
            MessagesService = new Mock<IMessagesService>();
            MessagesService
                .Setup(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Callback<Message, CancellationToken>((message, _) => SentMessage = message)
                .Returns(Task.CompletedTask);

            AuditWriter = new Mock<ISecurityAuditWriter>();
            AuditWriter
                .Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SecurityAuditWriteResult.Recorded);

            Mock<IRequestContext> requestContext = new();
            requestContext.SetupGet(context => context.TenantId).Returns("test-tenant");
            requestContext.SetupGet(context => context.RequestId).Returns("sender-test-request");

            DeliveryLogger = new RecordingLogger<AuthenticationOtpDeliveryService>();
            AuthenticationOtpDeliveryService delivery = new(
                AuditWriter.Object,
                requestContext.Object,
                new TestAuthenticationUtcClock(),
                DeliveryLogger);

            SenderLogger = new RecordingLogger<EmailAuthenticationCodeSender>();
            ServiceCollection localizationServices = new();
            _ = localizationServices.AddLogging();
            _ = localizationServices.AddLocalization();
            LocalizationServices = localizationServices.BuildServiceProvider();
            IStringLocalizer<AuthenticationEmailCodeResources> resources =
                LocalizationServices.GetRequiredService<IStringLocalizer<AuthenticationEmailCodeResources>>();
            Sender = new EmailAuthenticationCodeSender(
                MessagesService.Object,
                delivery,
                Options.Create(new DeliveredCodeLogOptions { Enabled = deliveredCodeLoggingEnabled }),
                resources,
                SenderLogger);
        }

        public Mock<IMessagesService> MessagesService { get; }
        public Mock<ISecurityAuditWriter> AuditWriter { get; }
        public ServiceProvider LocalizationServices { get; }
        public RecordingLogger<AuthenticationOtpDeliveryService> DeliveryLogger { get; }
        public RecordingLogger<EmailAuthenticationCodeSender> SenderLogger { get; }
        public EmailAuthenticationCodeSender Sender { get; }
        public Message? SentMessage { get; private set; }

        public void Dispose() => LocalizationServices.Dispose();
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

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
