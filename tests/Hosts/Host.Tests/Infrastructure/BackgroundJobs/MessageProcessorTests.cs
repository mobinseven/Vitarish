using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Communications;
using CommunicationsMessage = Vitarish.Communications.Domain.Models.Message;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class MessageProcessorTests
{
    private readonly Mock<ILogger<MessageProcessor>> mockLogger;
    private readonly Mock<IMessagesService> mockMessagesService;
    private readonly MessageProcessor sut;

    public MessageProcessorTests()
    {
        mockLogger = new Mock<ILogger<MessageProcessor>>();
        mockMessagesService = new Mock<IMessagesService>();
        sut = new MessageProcessor(mockLogger.Object, mockMessagesService.Object);
    }

    [Fact]
    public async Task ProcessMessage_WithEmailMessage_SendsEmail()
    {
        EmailMessage message = new(
            To: "test@example.com",
            Subject: "Test Subject",
            TextBody: "Test Text",
            HtmlBody: "<p>Test HTML</p>"
        );

        await sut.ProcessMessage(message);

        mockMessagesService.Verify(
            x => x.SendEmail(
                It.Is<CommunicationsMessage>(m =>
                    m.Recipient == message.To &&
                    m.Subject == message.Subject &&
                    m.Body == message.TextBody &&
                    m.HtmlBody == message.HtmlBody)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithSmsMessage_SendsSms()
    {
        SmsMessage message = new(
            PhoneNumber: "+1234567890",
            Code: "123456"
        );

        await sut.ProcessMessage(message);

        mockMessagesService.Verify(
            x => x.SendSmsCode(
                It.Is<CommunicationsMessage>(m =>
                    m.Recipient == message.PhoneNumber &&
                    m.Body == message.Code),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        Message? message = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ProcessMessage(message!));
    }

    [Fact]
    public async Task ProcessMessage_WhenEmailSendingFails_ThrowsException()
    {
        EmailMessage message = new(
            To: "test@example.com",
            Subject: "Test Subject",
            TextBody: "Test Text",
            HtmlBody: "<p>Test HTML</p>"
        );

        Exception expectedException = new("Email send failed");
        _ = mockMessagesService
            .Setup(x => x.SendEmail(It.IsAny<CommunicationsMessage>()))
            .ThrowsAsync(expectedException);

        Exception exception = await Assert.ThrowsAsync<Exception>(() =>
            sut.ProcessMessage(message));

        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task ProcessMessage_WhenSmsSendingFails_ThrowsException()
    {
        SmsMessage message = new(
            PhoneNumber: "+1234567890",
            Code: "123456"
        );

        Exception expectedException = new("SMS send failed");
        _ = mockMessagesService
            .Setup(x => x.SendSmsCode(
                It.IsAny<CommunicationsMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        Exception exception = await Assert.ThrowsAsync<Exception>(() =>
            sut.ProcessMessage(message));

        Assert.Equal(expectedException, exception);
    }
}
