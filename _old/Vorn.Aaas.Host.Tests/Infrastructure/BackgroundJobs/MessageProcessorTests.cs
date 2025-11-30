using Microsoft.Extensions.Logging;
using Moq;
using Vorn.Aaas.Host.Infrastructure.BackgroundJobs;
using Vorn.Caas.Client.Abstractions;

namespace Vorn.Aaas.Host.Tests.Infrastructure.BackgroundJobs;

public class MessageProcessorTests
{
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly Mock<IMessageClient> _mockMessageClient;
    private readonly MessageProcessor _sut;

    public MessageProcessorTests()
    {
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        _mockMessageClient = new Mock<IMessageClient>();
        _sut = new MessageProcessor(_mockLogger.Object, _mockMessageClient.Object);
    }

    [Fact]
    public async Task ProcessMessage_WithEmailMessage_SendsEmail()
    {
        // Arrange
        EmailMessage message = new(
            To: "test@example.com",
            Subject: "Test Subject",
            TextBody: "Test Text",
            HtmlBody: "<p>Test HTML</p>"
        );

        // Act
        await _sut.ProcessMessage(message);

        // Assert
        _mockMessageClient.Verify(
            x => x.SendEmailAsync(
                message.To,
                message.Subject,
                message.TextBody,
                message.HtmlBody,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithSmsMessage_SendsSms()
    {
        // Arrange
        SmsMessage message = new(
            PhoneNumber: "+1234567890",
            Code: "123456"
        );

        // Act
        await _sut.ProcessMessage(message);

        // Assert
        _mockMessageClient.Verify(
            x => x.SendSmsCodeAsync(
                message.PhoneNumber,
                message.Code,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        Message? message = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ProcessMessage(message!));
    }

    [Fact]
    public async Task ProcessMessage_WhenEmailSendingFails_ThrowsException()
    {
        // Arrange
        EmailMessage message = new(
            To: "test@example.com",
            Subject: "Test Subject",
            TextBody: "Test Text",
            HtmlBody: "<p>Test HTML</p>"
        );

        Exception expectedException = new("Email send failed");
        _mockMessageClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        Exception exception = await Assert.ThrowsAsync<Exception>(() =>
            _sut.ProcessMessage(message));

        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task ProcessMessage_WhenSmsSendingFails_ThrowsException()
    {
        // Arrange
        SmsMessage message = new(
            PhoneNumber: "+1234567890",
            Code: "123456"
        );

        Exception expectedException = new("SMS send failed");
        _mockMessageClient
            .Setup(x => x.SendSmsCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        Exception exception = await Assert.ThrowsAsync<Exception>(() =>
            _sut.ProcessMessage(message));

        Assert.Equal(expectedException, exception);
    }
}
