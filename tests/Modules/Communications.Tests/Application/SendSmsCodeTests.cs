namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SendSmsCode"/> command.
/// </summary>
public sealed class SendSmsCodeTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidMessage_CallsService()
    {
        // Arrange
        bool codeSent = false;
        Message? capturedMessage = null;
        FakeMessagesService service = new()
        {
            SendSmsCodeDelegate = (msg, _) =>
            {
                codeSent = true;
                capturedMessage = msg;
                return Task.CompletedTask;
            }
        };
        Message message = new() { Recipient = "+15551234567", Body = "123456" };

        // Act
        await SendSmsCode.ExecuteAsync(service, message);

        // Assert
        Assert.True(codeSent);
        Assert.NotNull(capturedMessage);
        Assert.Equal(message.Recipient, capturedMessage.Recipient);
        Assert.Equal(message.Body, capturedMessage.Body);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesNewMessageWithOnlyRecipientAndBody()
    {
        // Arrange
        Message? capturedMessage = null;
        FakeMessagesService service = new()
        {
            SendSmsCodeDelegate = (msg, _) =>
            {
                capturedMessage = msg;
                return Task.CompletedTask;
            }
        };
        Message message = new()
        {
            Sender = "sender@example.com",
            Recipient = "+15551234567",
            Subject = "Should be ignored",
            HtmlBody = "<p>Should be ignored</p>",
            Body = "123456"
        };

        // Act
        await SendSmsCode.ExecuteAsync(service, message);

        // Assert - The command creates a new message with only Recipient and Body
        Assert.NotNull(capturedMessage);
        Assert.Equal("+15551234567", capturedMessage.Recipient);
        Assert.Equal("123456", capturedMessage.Body);
        Assert.Null(capturedMessage.Sender);
        Assert.Null(capturedMessage.Subject);
        Assert.Null(capturedMessage.HtmlBody);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        Message message = new() { Recipient = "+15551234567", Body = "123456" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => SendSmsCode.ExecuteAsync(null!, message));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        FakeMessagesService service = new();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => SendSmsCode.ExecuteAsync(service, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithInvalidRecipient_ThrowsArgumentException(string? recipient)
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = recipient, Body = "123456" };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSmsCode.ExecuteAsync(service, message));
        Assert.Contains("Phone is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithInvalidBody_ThrowsArgumentException(string? body)
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = "+15551234567", Body = body };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSmsCode.ExecuteAsync(service, message));
        Assert.Contains("Code is required", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = "+15551234567", Body = "123456" };
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => SendSmsCode.ExecuteAsync(service, message, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken observedToken = default;
        FakeMessagesService service = new()
        {
            SendSmsCodeDelegate = (_, token) =>
            {
                observedToken = token;
                return Task.CompletedTask;
            }
        };
        Message message = new() { Recipient = "+15551234567", Body = "123456" };

        // Act
        await SendSmsCode.ExecuteAsync(service, message, cts.Token);

        // Assert
        Assert.Equal(cts.Token, observedToken);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesRecipientBeforeBody()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = null, Body = null }; // Both invalid

        // Act & Assert - Phone validation should happen first
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSmsCode.ExecuteAsync(service, message));
        Assert.Contains("Phone is required", exception.Message);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12345678")]
    [InlineData("ABCD123")]
    public async Task ExecuteAsync_AcceptsVariousCodeFormats(string code)
    {
        // Arrange
        string? capturedCode = null;
        FakeMessagesService service = new()
        {
            SendSmsCodeDelegate = (msg, _) =>
            {
                capturedCode = msg.Body;
                return Task.CompletedTask;
            }
        };
        Message message = new() { Recipient = "+15551234567", Body = code };

        // Act
        await SendSmsCode.ExecuteAsync(service, message);

        // Assert
        Assert.Equal(code, capturedCode);
    }

    private sealed class FakeMessagesService : IMessagesService
    {
        public Func<Message, CancellationToken, Task>? SendSmsCodeDelegate { get; set; }

        public Task SendEmail(Message message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmsSendResult("0", message.Recipient, 0m, null, null, null, null));

        public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) =>
            SendSmsCodeDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;

        public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmsDeliveryState(messageId.ToString(), null, null, null, null, null, null, null, null));
        public Task SendSmsReminder(string recipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
