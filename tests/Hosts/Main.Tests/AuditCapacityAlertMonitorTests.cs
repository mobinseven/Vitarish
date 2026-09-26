using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Server;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Options;

namespace Main.Tests;

public sealed class AuditCapacityAlertMonitorTests
{
    [Fact]
    public async Task Threshold_alert_is_sent_once_until_storage_drops_below_threshold()
    {
        SequenceStorageSizeReader sizeReader = new(100, 100, 99, 100);
        Mock<IMessagesService> messages = CreateMessagesService();
        using ServiceProvider services = CreateServices(sizeReader, messages.Object);
        AuditCapacityAlertMonitor monitor = CreateMonitor(services, threshold: 100);

        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);

        messages.Verify(service => service.SendEmail(
            It.Is<Message>(message =>
                message.Subject == "Audit storage capacity warning" &&
                message.Recipient == "admin@vitarish.com"),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        messages.Verify(service => service.SendSms(
            It.Is<Message>(message => message.Recipient == "+989120000000"),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Failed_check_alert_is_rearmed_after_a_successful_check()
    {
        SequenceStorageSizeReader sizeReader = new(
            () => throw new InvalidOperationException("private database detail"),
            () => throw new InvalidOperationException("private database detail"),
            () => 10,
            () => throw new InvalidOperationException("private database detail"));
        Mock<IMessagesService> messages = CreateMessagesService();
        using ServiceProvider services = CreateServices(sizeReader, messages.Object);
        AuditCapacityAlertMonitor monitor = CreateMonitor(services, threshold: 100);

        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);

        messages.Verify(service => service.SendEmail(
            It.Is<Message>(message => message.Subject == "Audit storage check failed"),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        messages.Verify(service => service.SendSms(
            It.IsAny<Message>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Email_delivery_failure_does_not_skip_sms_attempt()
    {
        SequenceStorageSizeReader sizeReader = new(100);
        Mock<IMessagesService> messages = CreateMessagesService();
        _ = messages.Setup(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private delivery detail"));
        using ServiceProvider services = CreateServices(sizeReader, messages.Object);
        AuditCapacityAlertMonitor monitor = CreateMonitor(services, threshold: 100);

        await monitor.CheckOnceAsync(CancellationToken.None);

        messages.Verify(service => service.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sms_delivery_failure_does_not_repeat_email_or_alert_attempts_in_same_episode()
    {
        SequenceStorageSizeReader sizeReader = new(100, 100);
        Mock<IMessagesService> messages = CreateMessagesService();
        _ = messages.Setup(service => service.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private delivery detail"));
        using ServiceProvider services = CreateServices(sizeReader, messages.Object);
        AuditCapacityAlertMonitor monitor = CreateMonitor(services, threshold: 100);

        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);

        messages.Verify(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
        messages.Verify(service => service.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Production_options_require_both_recipients()
    {
        Mock<IHostEnvironment> environment = new();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        AuditCapacityAlertOptionsValidator validator = new(environment.Object, openApiGeneration: false);

        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new AuditCapacityAlertOptions
            {
                EmailRecipient = "admin@vitarish.com"
            });

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain(AuditCapacityAlertOptions.SmsRecipientKey);
    }

    [Fact]
    public async Task Production_host_start_fails_when_alert_recipients_are_invalid()
    {
        using IHost host = new HostBuilder()
            .UseEnvironment(Environments.Production)
            .ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [AuditCapacityAlertOptions.EmailRecipientKey] = "not-an-email",
                    [AuditCapacityAlertOptions.SmsRecipientKey] = "replace-with-admin-phone"
                }))
            .ConfigureServices((context, services) =>
            {
                _ = services.AddAuditCapacityAlertMonitoring(context.HostingEnvironment, openApiGeneration: false);
            })
            .Build();

        OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());

        exception.Failures.Should().Contain(failure =>
            failure.Contains(AuditCapacityAlertOptions.EmailRecipientKey, StringComparison.Ordinal));
        exception.Failures.Should().Contain(failure =>
            failure.Contains(AuditCapacityAlertOptions.SmsRecipientKey, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("admin@example.invalid", "+989120000000")]
    [InlineData("not-an-email", "+989120000000")]
    [InlineData("admin@vitarish.com", "replace-with-admin-phone")]
    [InlineData("admin@vitarish.com", "+15555550100")]
    public void Production_options_reject_placeholder_or_malformed_recipients(string email, string phone)
    {
        Mock<IHostEnvironment> environment = new();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        AuditCapacityAlertOptionsValidator validator = new(environment.Object, openApiGeneration: false);

        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new AuditCapacityAlertOptions { EmailRecipient = email, SmsRecipient = phone });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Production_options_accept_non_placeholder_email_and_e164_phone()
    {
        Mock<IHostEnvironment> environment = new();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        AuditCapacityAlertOptionsValidator validator = new(environment.Object, openApiGeneration: false);

        ValidateOptionsResult result = validator.Validate(
            Options.DefaultName,
            new AuditCapacityAlertOptions
            {
                EmailRecipient = "admin@vitarish.com",
                SmsRecipient = "+989120000000"
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Hosted_monitor_checks_storage_immediately_when_host_starts()
    {
        SignalingStorageSizeReader sizeReader = new();
        Mock<IMessagesService> messages = CreateMessagesService();
        using IHost host = new HostBuilder()
            .UseEnvironment(Environments.Production)
            .ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [AuditCapacityAlertOptions.EmailRecipientKey] = "admin@vitarish.com",
                    [AuditCapacityAlertOptions.SmsRecipientKey] = "+989120000000"
                }))
            .ConfigureServices(services =>
            {
                _ = services.AddSingleton(sizeReader);
                _ = services.AddScoped<IAuditStorageSizeReader>(provider => provider.GetRequiredService<SignalingStorageSizeReader>());
                _ = services.AddSingleton(messages.Object);
                _ = services.AddSingleton<IOptions<SecurityAuditOptions>>(
                    Options.Create(new SecurityAuditOptions { CapacityWarningBytes = 100 }));
            })
            .ConfigureServices((context, services) =>
            {
                _ = services.AddAuditCapacityAlertMonitoring(context.HostingEnvironment, openApiGeneration: false);
            })
            .Build();

        await host.StartAsync();
        await sizeReader.FirstCall.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await host.StopAsync();

        sizeReader.CallCount.Should().Be(1);
        messages.Verify(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
        messages.Verify(service => service.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AuditCapacityAlertMonitor CreateMonitor(IServiceProvider services, long threshold) =>
        new(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SecurityAuditOptions { CapacityWarningBytes = threshold }),
            Options.Create(new AuditCapacityAlertOptions
            {
                EmailRecipient = "admin@vitarish.com",
                SmsRecipient = "+989120000000"
            }),
            NullLogger<AuditCapacityAlertMonitor>.Instance);

    private static ServiceProvider CreateServices(IAuditStorageSizeReader reader, IMessagesService messages)
    {
        ServiceCollection services = new();
        _ = services.AddSingleton(reader);
        _ = services.AddSingleton(messages);
        return services.BuildServiceProvider();
    }

    private sealed class SignalingStorageSizeReader : IAuditStorageSizeReader
    {
        public TaskCompletionSource FirstCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CallCount { get; private set; }

        public Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            FirstCall.TrySetResult();
            return Task.FromResult(10L);
        }
    }

    private static Mock<IMessagesService> CreateMessagesService()
    {
        Mock<IMessagesService> messages = new();
        _ = messages.Setup(service => service.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ = messages.Setup(service => service.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("test-id", null, 0, null, null, null, null));
        return messages;
    }

    private sealed class SequenceStorageSizeReader(params Func<long>[] results) : IAuditStorageSizeReader
    {
        private int index;

        public Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Func<long> result = results[Math.Min(index++, results.Length - 1)];
            return Task.FromResult(result());
        }

        public SequenceStorageSizeReader(params long[] sizes)
            : this(sizes.Select<long, Func<long>>(size => () => size).ToArray())
        {
        }
    }
}
