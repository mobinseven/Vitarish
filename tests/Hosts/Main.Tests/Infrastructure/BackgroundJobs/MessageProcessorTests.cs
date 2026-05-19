using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs.Communications;
using DeliveryMessage = Vitarish.Communications.Domain.Models.Message;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class MessageProcessorTests
{
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly Mock<IMessagesService> _mockMessagesService;
    private readonly Mock<ICommunicationsModule> _mockCommunicationModule;
    private readonly MessageProcessor _sut;

    public MessageProcessorTests()
    {
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        _mockMessagesService = new Mock<IMessagesService>();
        _mockCommunicationModule = new Mock<ICommunicationsModule>();
        _sut = new MessageProcessor(_mockLogger.Object, _mockMessagesService.Object, _mockCommunicationModule.Object);
    }

    [Fact]
    public async Task ProcessCommand_WithEmailCommand_SendsEmail()
    {
        EmailCommunicationCommand command = new("test@example.com", "Test Subject", "Test Text", "<p>Test HTML</p>");

        await _sut.ProcessCommand(command);

        _mockMessagesService.Verify(
            x => x.SendEmail(
                It.Is<DeliveryMessage>(m =>
                    m.Recipient == command.Recipient &&
                    m.Subject == command.Subject &&
                    m.Body == command.TextBody &&
                    m.HtmlBody == command.HtmlBody),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommand_WithSmsCommand_SendsSms()
    {
        SmsCommunicationCommand command = new("+1234567890", "Test SMS");

        _ = _mockMessagesService
            .Setup(x => x.SendSms(It.IsAny<DeliveryMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("queued", command.Recipient, 0m, null, null, null, null));

        await _sut.ProcessCommand(command);

        _mockMessagesService.Verify(
            x => x.SendSms(
                It.Is<DeliveryMessage>(m =>
                    m.Recipient == command.Recipient &&
                    m.Body == command.Body),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommand_WithSmsCodeCommand_SendsSmsCode()
    {
        SmsCodeCommunicationCommand command = new("+1234567890", "123456");

        await _sut.ProcessCommand(command);

        _mockMessagesService.Verify(
            x => x.SendSmsCode(
                It.Is<DeliveryMessage>(m =>
                    m.Recipient == command.Recipient &&
                    m.Body == command.Code),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommand_WithSmsReminderCommand_SendsSmsReminder()
    {
        SmsReminderCommunicationCommand command = new("+1234567890", "Project", "Procedure", "Event", "Tenant");

        await _sut.ProcessCommand(command);

        _mockMessagesService.Verify(
            x => x.SendSmsReminder(
                command.Recipient,
                command.Project,
                command.Procedure,
                command.Event,
                command.Tenant,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessCommand_WithNullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ProcessCommand(null!));
    }

    [Fact]
    public async Task ProcessCommand_WhenEmailSendingFails_ThrowsException()
    {
        EmailCommunicationCommand command = new("test@example.com", "Test Subject", "Test Text", "<p>Test HTML</p>");
        Exception expectedException = new("Email send failed");

        _ = _mockMessagesService
            .Setup(x => x.SendEmail(It.IsAny<DeliveryMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        Exception exception = await Assert.ThrowsAsync<Exception>(() => _sut.ProcessCommand(command));

        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task ProcessCommand_WhenSmsSendingFails_ThrowsException()
    {
        SmsCommunicationCommand command = new("+1234567890", "Test SMS");
        Exception expectedException = new("SMS send failed");

        _ = _mockMessagesService
            .Setup(x => x.SendSms(It.IsAny<DeliveryMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        Exception exception = await Assert.ThrowsAsync<Exception>(() => _sut.ProcessCommand(command));

        Assert.Equal(expectedException, exception);
    }
}
