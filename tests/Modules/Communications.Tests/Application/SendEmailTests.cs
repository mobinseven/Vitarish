namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SendEmail"/> command.
/// </summary>
public sealed class SendEmailTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidMessage_CallsService()
    {
        // Arrange
        bool emailSent = false;
        Message? capturedMessage = null;
        FakeMessagesService service = new()
        {
            SendEmailDelegate = (msg, _) =>
            {
                emailSent = true;
                capturedMessage = msg;
                return Task.CompletedTask;
            }
        };
        Message message = new() { Recipient = "test@example.com", Subject = "Test", Body = "Hello" };

        // Act
        await SendEmail.ExecuteAsync(service, message);

        // Assert
        Assert.True(emailSent);
        Assert.Same(message, capturedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        Message message = new() { Recipient = "test@example.com" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => SendEmail.ExecuteAsync(null!, message));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        FakeMessagesService service = new();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => SendEmail.ExecuteAsync(service, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithInvalidRecipient_ThrowsArgumentException(string? recipient)
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = recipient, Subject = "Test", Body = "Hello" };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendEmail.ExecuteAsync(service, message));
        Assert.Contains("Recipient is required", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = "test@example.com" };
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => SendEmail.ExecuteAsync(service, message, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRecipient_SucceedsEvenWithoutSubjectOrBody()
    {
        // Arrange
        bool emailSent = false;
        FakeMessagesService service = new()
        {
            SendEmailDelegate = (_, _) =>
            {
                emailSent = true;
                return Task.CompletedTask;
            }
        };
        Message message = new() { Recipient = "test@example.com" };

        // Act
        await SendEmail.ExecuteAsync(service, message);

        // Assert
        Assert.True(emailSent);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesBeforeCheckingCancellation()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = null }; // Invalid recipient
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert - validation should happen first and throw ArgumentException, not OperationCanceledException
        await Assert.ThrowsAsync<ArgumentException>(
            () => SendEmail.ExecuteAsync(service, message, cts.Token));
    }

    private sealed class FakeMessagesService : IMessagesService
    {
        public Func<Message, CancellationToken, Task>? SendEmailDelegate { get; set; }

        public Task SendEmail(Message message, CancellationToken cancellationToken = default) =>
            SendEmailDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;

        public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmsSendResult("0", message.Recipient, 0m, null, null, null, null));

        public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmsDeliveryState(messageId.ToString(), null, null, null, null, null, null, null, null));
        public Task SendSmsReminder(string recipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
