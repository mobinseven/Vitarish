using FluentAssertions;
using Vitarish.Communications.Hosting.Hubs;

namespace Vitarish.Communications.Tests.Hosting;

/// <summary>
/// Unit tests for the <see cref="MessagesHub"/> class.
/// </summary>
public sealed class MessagesHubTests
{
    [Fact]
    public void Constructor_WithValidService_CreatesInstance()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();

        // Act
        var hub = new MessagesHub(mockService.Object);

        // Assert
        hub.Should().NotBeNull();
    }

    [Fact]
    public async Task SendEmailAsync_CallsServiceSendEmail()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };
        var cancellationToken = CancellationToken.None;

        mockService
            .Setup(s => s.SendEmail(It.IsAny<Message>()))
            .Returns(Task.CompletedTask);

        var hub = new MessagesHub(mockService.Object);

        // Act
        await hub.SendEmailAsync(message, cancellationToken);

        // Assert
        mockService.Verify(s => s.SendEmail(message), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "test@example.com",
            Body = "Test Body"
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var hub = new MessagesHub(mockService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => hub.SendEmailAsync(message, cts.Token));

        mockService.Verify(s => s.SendEmail(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task SendSmsAsync_CallsServiceSendSms()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "+15551234567",
            Body = "Test SMS"
        };
        var cancellationToken = CancellationToken.None;
        var expectedResult = new SmsSendResult(
            MessageId: "12345",
            Recipient: "+15551234567",
            Cost: 0.05m,
            PackId: Guid.NewGuid(),
            SubmittedAt: DateTimeOffset.UtcNow,
            RelatedMessageIds: null,
            Metadata: null);

        mockService
            .Setup(s => s.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var hub = new MessagesHub(mockService.Object);

        // Act
        var result = await hub.SendSmsAsync(message, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.MessageId.Should().Be("12345");
        result.Recipient.Should().Be("+15551234567");
        result.Cost.Should().Be(0.05m);
        mockService.Verify(s => s.SendSms(message, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_PropagatesCancellationToken()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "+15551234567",
            Body = "Test SMS"
        };
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        mockService
            .Setup(s => s.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("123", null, 0, null, null, null, null));

        var hub = new MessagesHub(mockService.Object);

        // Act
        await hub.SendSmsAsync(message, cancellationToken);

        // Assert
        mockService.Verify(s => s.SendSms(message, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendSmsCodeAsync_CallsServiceSendSmsCode()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "+15551234567",
            Body = "123456"
        };
        var cancellationToken = CancellationToken.None;

        mockService
            .Setup(s => s.SendSmsCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new MessagesHub(mockService.Object);

        // Act
        await hub.SendSmsCodeAsync(message, cancellationToken);

        // Assert
        mockService.Verify(s => s.SendSmsCode(message, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendSmsCodeAsync_PropagatesCancellationToken()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "+15551234567",
            Body = "654321"
        };
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        mockService
            .Setup(s => s.SendSmsCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new MessagesHub(mockService.Object);

        // Act
        await hub.SendSmsCodeAsync(message, cancellationToken);

        // Assert
        mockService.Verify(s => s.SendSmsCode(message, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_CallsServiceGetSmsDeliveryStateAsync()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var messageId = 12345L;
        var cancellationToken = CancellationToken.None;
        var expectedState = new SmsDeliveryState(
            MessageId: "12345",
            Recipient: "+15551234567",
            StatusCode: 1,
            Status: "Delivered",
            MessageText: "Test message",
            Cost: 0.05m,
            SentAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            DeliveredAt: DateTimeOffset.UtcNow,
            Metadata: null);

        mockService
            .Setup(s => s.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);

        var hub = new MessagesHub(mockService.Object);

        // Act
        var result = await hub.GetSmsDeliveryStateAsync(messageId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.MessageId.Should().Be("12345");
        result.Recipient.Should().Be("+15551234567");
        result.Status.Should().Be("Delivered");
        result.Cost.Should().Be(0.05m);
        mockService.Verify(s => s.GetSmsDeliveryStateAsync(messageId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_PropagatesCancellationToken()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var messageId = 67890L;
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        mockService
            .Setup(s => s.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryState("123", null, null, null, null, null, null, null, null));

        var hub = new MessagesHub(mockService.Object);

        // Act
        await hub.GetSmsDeliveryStateAsync(messageId, cancellationToken);

        // Assert
        mockService.Verify(s => s.GetSmsDeliveryStateAsync(messageId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithNullMessage_ServiceHandlesValidation()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        mockService
            .Setup(s => s.SendEmail(null!))
            .ThrowsAsync(new ArgumentNullException("message"));

        var hub = new MessagesHub(mockService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => hub.SendEmailAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendSmsAsync_WithNullMessage_ServiceHandlesValidation()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        mockService
            .Setup(s => s.SendSms(null!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("message"));

        var hub = new MessagesHub(mockService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => hub.SendSmsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendSmsCodeAsync_WithNullMessage_ServiceHandlesValidation()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        mockService
            .Setup(s => s.SendSmsCode(null!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("message"));

        var hub = new MessagesHub(mockService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => hub.SendSmsCodeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_WithInvalidMessageId_ServiceHandlesValidation()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        mockService
            .Setup(s => s.GetSmsDeliveryStateAsync(0L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("messageId"));

        var hub = new MessagesHub(mockService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => hub.GetSmsDeliveryStateAsync(0L, CancellationToken.None));
    }

    [Fact]
    public async Task SendSmsAsync_ReturnsResultFromService()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var message = new Message
        {
            Recipient = "+19995551234",
            Body = "Another test"
        };
        var packId = Guid.NewGuid();
        var submittedAt = DateTimeOffset.UtcNow;
        var relatedIds = new List<string> { "rel1", "rel2" };
        var metadata = new Dictionary<string, string?> { ["key"] = "value" };

        var expectedResult = new SmsSendResult(
            MessageId: "msg-999",
            Recipient: "+19995551234",
            Cost: 0.10m,
            PackId: packId,
            SubmittedAt: submittedAt,
            RelatedMessageIds: relatedIds,
            Metadata: metadata);

        mockService
            .Setup(s => s.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var hub = new MessagesHub(mockService.Object);

        // Act
        var result = await hub.SendSmsAsync(message, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MessageId.Should().Be("msg-999");
        result.Recipient.Should().Be("+19995551234");
        result.Cost.Should().Be(0.10m);
        result.PackId.Should().Be(packId);
        result.SubmittedAt.Should().Be(submittedAt);
        result.RelatedMessageIds.Should().BeEquivalentTo(relatedIds);
        result.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_ReturnsCompleteStateFromService()
    {
        // Arrange
        var mockService = new Mock<IMessagesService>();
        var messageId = 555L;
        var sentAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var deliveredAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var metadata = new Dictionary<string, string?> { ["carrier"] = "TestCarrier" };

        var expectedState = new SmsDeliveryState(
            MessageId: "555",
            Recipient: "+12223334444",
            StatusCode: 2,
            Status: "Delivered",
            MessageText: "Your code is 123456",
            Cost: 0.08m,
            SentAt: sentAt,
            DeliveredAt: deliveredAt,
            Metadata: metadata);

        mockService
            .Setup(s => s.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);

        var hub = new MessagesHub(mockService.Object);

        // Act
        var result = await hub.GetSmsDeliveryStateAsync(messageId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MessageId.Should().Be("555");
        result.Recipient.Should().Be("+12223334444");
        result.StatusCode.Should().Be(2);
        result.Status.Should().Be("Delivered");
        result.MessageText.Should().Be("Your code is 123456");
        result.Cost.Should().Be(0.08m);
        result.SentAt.Should().Be(sentAt);
        result.DeliveredAt.Should().Be(deliveredAt);
        result.Metadata.Should().BeEquivalentTo(metadata);
    }
}
