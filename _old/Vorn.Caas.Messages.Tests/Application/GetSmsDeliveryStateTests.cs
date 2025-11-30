namespace Vorn.Communications.Tests.Application;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Caas.Messages.Abstractions;
using Vorn.Caas.Messages.Models;
using Vorn.Caas.Messages.Queries;
using Xunit;

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

    private sealed class FakeMessagesService : IMessagesService
    {
        public Func<Message, CancellationToken, Task>? EmailDelegate { get; set; }

        public Func<Message, CancellationToken, Task<SmsSendResult>>? SmsDelegate { get; set; }

        public Func<Message, CancellationToken, Task>? SmsCodeDelegate { get; set; }

        public Func<long, CancellationToken, Task<SmsDeliveryState>>? DeliveryDelegate { get; set; }

        public Task SendEmail(Message message) =>
            EmailDelegate?.Invoke(message, CancellationToken.None) ?? Task.CompletedTask;

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
    }
}
