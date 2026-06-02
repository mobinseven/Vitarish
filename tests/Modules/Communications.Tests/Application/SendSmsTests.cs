namespace Communications.Tests.Application;

public sealed class SendSmsTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsResultFromService()
    {
        SmsSendResult expected = new(
            "42",
            "+10000000000",
            1200m,
            Guid.NewGuid(),
            SubmittedAt: null,
            RelatedMessageIds: new[] { "42" },
            Metadata: null);
        FakeMessagesService service = new()
        {
            SendSmsDelegate = (_, _) => Task.FromResult(expected)
        };
        Message message = new() { Recipient = "+10000000000", Body = "hello" };

        SmsSendResult result = await SendSms.ExecuteAsync(service, message);

        Assert.Equal(expected, result);
        Assert.Same(message, service.LastSmsMessage);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        using CancellationTokenSource cts = new();
        CancellationToken observed = default;
        FakeMessagesService service = new()
        {
            SendSmsDelegate = (_, token) =>
            {
                observed = token;
                return Task.FromResult(new SmsSendResult(
                    "1",
                    "+10000000000",
                    0m,
                    Guid.Empty,
                    SubmittedAt: null,
                    RelatedMessageIds: new[] { "1" },
                    Metadata: null));
            }
        };
        Message message = new() { Recipient = "+10000000000", Body = "hello" };

        _ = await SendSms.ExecuteAsync(service, message, cts.Token);

        Assert.Equal(cts.Token, observed);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledTokenThrows()
    {
        FakeMessagesService service = new();
        Message message = new() { Recipient = "+10000000000", Body = "hello" };
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => SendSms.ExecuteAsync(service, message, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        Message message = new() { Recipient = "+15551234567", Body = "hello" };

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => SendSms.ExecuteAsync(null!, message));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        FakeMessagesService service = new();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => SendSms.ExecuteAsync(service, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithInvalidRecipient_ThrowsArgumentException(string? recipient)
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = recipient, Body = "hello" };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSms.ExecuteAsync(service, message));
        Assert.Contains("Recipient is required", exception.Message);
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
            () => SendSms.ExecuteAsync(service, message));
        Assert.Contains("Body is required", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesRecipientBeforeBody()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = null, Body = null }; // Both invalid

        // Act & Assert - Recipient validation should happen first
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSms.ExecuteAsync(service, message));
        Assert.Contains("Recipient is required", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesBeforeCheckingCancellation()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = null, Body = "hello" }; // Invalid recipient
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert - validation should happen first and throw ArgumentException, not OperationCanceledException
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => SendSms.ExecuteAsync(service, message, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultCancellationToken_Succeeds()
    {
        // Arrange
        FakeMessagesService service = new();
        Message message = new() { Recipient = "+15551234567", Body = "hello" };

        // Act
        SmsSendResult result = await SendSms.ExecuteAsync(service, message);

        // Assert
        Assert.NotNull(result);
    }

    private sealed class FakeMessagesService : IMessagesService
    {
        public Func<Message, CancellationToken, Task<SmsSendResult>>? SendSmsDelegate { get; set; }

        public Message? LastSmsMessage { get; private set; }

        public Task SendEmail(Message message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default)
        {
            LastSmsMessage = message;
            return SendSmsDelegate is not null
                ? SendSmsDelegate(message, cancellationToken)
                : Task.FromResult(new SmsSendResult(
                "0",
                message.Recipient,
                0m,
                Guid.Empty,
                SubmittedAt: null,
                RelatedMessageIds: Array.Empty<string>(),
                Metadata: null));
        }

        public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SmsDeliveryState(
                messageId.ToString(),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        public Task SendSmsReminder(string recipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
