using Vitarish.Communications.Infrastructure.Services;

namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="MessagesService"/>.
/// </summary>
public sealed class MessagesServiceTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<ISmsProvider> mockProvider = new();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new MessagesService(null!, mockProvider.Object));
        Assert.Equal("providersOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSmsProvider_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new MessagesService(mockOptions.Object, null!));
        Assert.Equal("smsProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());
        Mock<ISmsProvider> mockProvider = new();

        // Act
        MessagesService service = new(mockOptions.Object, mockProvider.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task SendSms_DelegatesToSmsProvider()
    {
        // Arrange
        SmsSendResult expectedResult = new("123", "+15551234567", 100m, null, null, null, null);
        Message? capturedMessage = null;
        CancellationToken capturedToken = default;

        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((msg, token) =>
            {
                capturedMessage = msg;
                capturedToken = token;
            })
            .ReturnsAsync(expectedResult);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);
        Message message = new() { Recipient = "+15551234567", Body = "Hello" };
        using CancellationTokenSource cts = new();

        // Act
        SmsSendResult result = await service.SendSms(message, cts.Token);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Same(message, capturedMessage);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task SendSmsCode_DelegatesToSmsProvider()
    {
        // Arrange
        Message? capturedMessage = null;
        CancellationToken capturedToken = default;

        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.SendCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((msg, token) =>
            {
                capturedMessage = msg;
                capturedToken = token;
            })
            .Returns(Task.CompletedTask);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);
        Message message = new() { Recipient = "+15551234567", Body = "123456" };
        using CancellationTokenSource cts = new();

        // Act
        await service.SendSmsCode(message, cts.Token);

        // Assert
        Assert.Same(message, capturedMessage);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_DelegatesToSmsProvider()
    {
        // Arrange
        SmsDeliveryState expectedState = new("123", "+15551234567", 2, "Delivered", "Hello", 50m, null, null, null);
        long capturedMessageId = 0;
        CancellationToken capturedToken = default;

        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<long, CancellationToken>((id, token) =>
            {
                capturedMessageId = id;
                capturedToken = token;
            })
            .ReturnsAsync(expectedState);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);
        using CancellationTokenSource cts = new();

        // Act
        SmsDeliveryState result = await service.GetSmsDeliveryStateAsync(123, cts.Token);

        // Assert
        Assert.Equal(expectedState, result);
        Assert.Equal(123, capturedMessageId);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task SendSms_WithDefaultCancellationToken_Succeeds()
    {
        // Arrange
        SmsSendResult expectedResult = new("123", "+15551234567", 100m, null, null, null, null);
        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);
        Message message = new() { Recipient = "+15551234567", Body = "Hello" };

        // Act
        SmsSendResult result = await service.SendSms(message);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task SendSmsCode_WithDefaultCancellationToken_Succeeds()
    {
        // Arrange
        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.SendCode(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);
        Message message = new() { Recipient = "+15551234567", Body = "123456" };

        // Act & Assert (no exception thrown)
        await service.SendSmsCode(message);
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_WithDefaultCancellationToken_Succeeds()
    {
        // Arrange
        SmsDeliveryState expectedState = new("123", "+15551234567", 2, "Delivered", "Hello", 50m, null, null, null);
        Mock<ISmsProvider> mockProvider = new();
        _ = mockProvider
            .Setup(x => x.GetSmsDeliveryStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedState);

        Mock<IOptionsMonitor<ProvidersOptions>> mockOptions = new();
        _ = mockOptions.Setup(x => x.CurrentValue).Returns(new ProvidersOptions());

        MessagesService service = new(mockOptions.Object, mockProvider.Object);

        // Act
        SmsDeliveryState result = await service.GetSmsDeliveryStateAsync(123);

        // Assert
        Assert.Equal(expectedState, result);
    }
}
