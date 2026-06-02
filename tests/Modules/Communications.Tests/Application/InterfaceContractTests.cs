namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for abstraction interfaces to ensure they are correctly defined.
/// </summary>
public sealed class InterfaceContractTests
{
    [Fact]
    public void IMessagesService_HasExpectedMethods()
    {
        // Assert that the interface has the expected methods by checking if we can create a mock
        Mock<IMessagesService> mock = new();

        // Verify all methods are present
        _ = mock.Setup(x => x.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _ = mock.Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("1", null, 0, null, null, null, null));
        _ = mock.Setup(x => x.SendSmsCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ = mock.Setup(x => x.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryState("1", null, null, null, null, null, null, null, null));

        // Verify all methods can be invoked
        Assert.NotNull(mock.Object);
    }

    [Fact]
    public void ISmsProvider_HasExpectedMethods()
    {
        // Assert that the interface has the expected methods by checking if we can create a mock
        Mock<ISmsProvider> mock = new();

        // Verify all methods are present
        _ = mock.Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("1", null, 0, null, null, null, null));
        _ = mock.Setup(x => x.SendCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ = mock.Setup(x => x.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryState("1", null, null, null, null, null, null, null, null));

        // Verify all methods can be invoked
        Assert.NotNull(mock.Object);
    }

    [Fact]
    public async Task IMessagesService_SendEmail_AcceptsMessage()
    {
        // Arrange
        Mock<IMessagesService> mock = new();
        Message message = new() { Recipient = "test@example.com", Subject = "Test", Body = "Hello" };
        _ = mock.Setup(x => x.SendEmail(message, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await mock.Object.SendEmail(message);

        // Assert
        mock.Verify(x => x.SendEmail(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IMessagesService_SendSms_ReturnsSmsResult()
    {
        // Arrange
        Mock<IMessagesService> mock = new();
        Message message = new() { Recipient = "+15551234567", Body = "Hello" };
        SmsSendResult expected = new("123", "+15551234567", 100m, null, null, null, null);
        _ = mock.Setup(x => x.SendSms(message, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        SmsSendResult result = await mock.Object.SendSms(message);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task IMessagesService_SendSmsCode_CompletesSuccessfully()
    {
        // Arrange
        Mock<IMessagesService> mock = new();
        Message message = new() { Recipient = "+15551234567", Body = "123456" };
        _ = mock.Setup(x => x.SendSmsCode(message, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act & Assert (no exception)
        await mock.Object.SendSmsCode(message);
        mock.Verify(x => x.SendSmsCode(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IMessagesService_GetSmsDeliveryStateAsync_ReturnsDeliveryState()
    {
        // Arrange
        Mock<IMessagesService> mock = new();
        SmsDeliveryState expected = new("123", "+15551234567", 2, "Delivered", "Hello", 50m, null, null, null);
        _ = mock.Setup(x => x.GetSmsDeliveryStateAsync(123, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        SmsDeliveryState result = await mock.Object.GetSmsDeliveryStateAsync(123);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ISmsProvider_SendSms_ReturnsSmsResult()
    {
        // Arrange
        Mock<ISmsProvider> mock = new();
        Message message = new() { Recipient = "+15551234567", Body = "Hello" };
        SmsSendResult expected = new("123", "+15551234567", 100m, null, null, null, null);
        _ = mock.Setup(x => x.SendSms(message, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        SmsSendResult result = await mock.Object.SendSms(message);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ISmsProvider_SendCode_CompletesSuccessfully()
    {
        // Arrange
        Mock<ISmsProvider> mock = new();
        Message message = new() { Recipient = "+15551234567", Body = "123456" };
        _ = mock.Setup(x => x.SendCode(message, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act & Assert (no exception)
        await mock.Object.SendCode(message);
        mock.Verify(x => x.SendCode(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ISmsProvider_GetSmsDeliveryStateAsync_ReturnsDeliveryState()
    {
        // Arrange
        Mock<ISmsProvider> mock = new();
        SmsDeliveryState expected = new("123", "+15551234567", 2, "Delivered", "Hello", 50m, null, null, null);
        _ = mock.Setup(x => x.GetSmsDeliveryStateAsync(123, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        SmsDeliveryState result = await mock.Object.GetSmsDeliveryStateAsync(123);

        // Assert
        Assert.Equal(expected, result);
    }
}
