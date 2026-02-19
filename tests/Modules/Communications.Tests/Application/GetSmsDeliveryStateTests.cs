namespace Communications.Tests.Application;

public sealed class GetSmsDeliveryStateTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsStateFromService()
    {
        SmsDeliveryState expected = new(
            "10",
            "+10000000000",
            2,
            "موفق",
            "hello",
            1.2m,
            DateTimeOffset.FromUnixTimeSeconds(1_628_683_626).ToLocalTime(),
            DateTimeOffset.FromUnixTimeSeconds(1_628_683_629).ToLocalTime(),
            Metadata: new Dictionary<string, string?> { ["lineNumber"] = "30004505000017" });
        FakeMessagesService service = new()
        {
            DeliveryDelegate = (_, _) => Task.FromResult(expected)
        };

        SmsDeliveryState result = await GetSmsDeliveryState.ExecuteAsync(service, 10);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenIdInvalid()
    {
        FakeMessagesService service = new();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => GetSmsDeliveryState.ExecuteAsync(service, 0));
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        using CancellationTokenSource cts = new();
        CancellationToken observed = default;
        FakeMessagesService service = new()
        {
            DeliveryDelegate = (_, token) =>
            {
                observed = token;
                SmsDeliveryState state = new(
                    "5",
                    "+10000000000",
                    1,
                    "موفق",
                    "hello",
                    null,
                    null,
                    null,
                    null);
                return Task.FromResult(state);
            }
        };

        await GetSmsDeliveryState.ExecuteAsync(service, 5, cts.Token);

        Assert.Equal(cts.Token, observed);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullService_ThrowsArgumentNullException() =>
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetSmsDeliveryState.ExecuteAsync(null!, 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(long.MinValue)]
    public async Task ExecuteAsync_WithNonPositiveMessageId_ThrowsArgumentOutOfRangeException(long messageId)
    {
        // Arrange
        FakeMessagesService service = new();

        // Act & Assert
        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => GetSmsDeliveryState.ExecuteAsync(service, messageId));
        Assert.Contains("Message ID must be a positive value", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        FakeMessagesService service = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => GetSmsDeliveryState.ExecuteAsync(service, 1, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesBeforeCheckingCancellation()
    {
        // Arrange
        FakeMessagesService service = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert - validation should happen first and throw ArgumentOutOfRangeException, not OperationCanceledException
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => GetSmsDeliveryState.ExecuteAsync(service, 0, cts.Token));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999999)]
    [InlineData(long.MaxValue)]
    public async Task ExecuteAsync_WithPositiveMessageId_CallsService(long messageId)
    {
        // Arrange
        long? capturedMessageId = null;
        FakeMessagesService service = new()
        {
            DeliveryDelegate = (id, _) =>
            {
                capturedMessageId = id;
                return Task.FromResult(new SmsDeliveryState(id.ToString(), null, null, null, null, null, null, null, null));
            }
        };

        // Act
        await GetSmsDeliveryState.ExecuteAsync(service, messageId);

        // Assert
        Assert.Equal(messageId, capturedMessageId);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultCancellationToken_Succeeds()
    {
        // Arrange
        FakeMessagesService service = new();

        // Act
        SmsDeliveryState result = await GetSmsDeliveryState.ExecuteAsync(service, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.MessageId);
    }

    private sealed class FakeMessagesService : IMessagesService
    {
        public Func<Message, CancellationToken, Task>? EmailDelegate { get; set; }

        public Func<Message, CancellationToken, Task<SmsSendResult>>? SmsDelegate { get; set; }

        public Func<Message, CancellationToken, Task>? SmsCodeDelegate { get; set; }

        public Func<long, CancellationToken, Task<SmsDeliveryState>>? DeliveryDelegate { get; set; }

        public Task SendEmail(Message message, CancellationToken cancellationToken = default) =>
            EmailDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;

        public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
            SmsDelegate?.Invoke(message, cancellationToken)
            ?? Task.FromResult(new SmsSendResult(
                "0",
                message.Recipient,
                0m,
                Guid.Empty,
                SubmittedAt: null,
                RelatedMessageIds: Array.Empty<string>(),
                Metadata: null));

        public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) =>
            SmsCodeDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;

        public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
            DeliveryDelegate?.Invoke(messageId, cancellationToken)
            ?? Task.FromResult(new SmsDeliveryState(
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
