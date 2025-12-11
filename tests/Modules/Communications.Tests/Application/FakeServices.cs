namespace Vitarish.Communications.Tests.Application;

/// <summary>
/// Fake implementation of <see cref="IMessagesService"/> for testing purposes.
/// </summary>
internal sealed class FakeMessagesService : IMessagesService
{
    public Func<Message, CancellationToken, Task>? SendEmailDelegate { get; set; }
    public Func<Message, CancellationToken, Task<SmsSendResult>>? SendSmsDelegate { get; set; }
    public Func<Message, CancellationToken, Task>? SendSmsCodeDelegate { get; set; }
    public Func<long, CancellationToken, Task<SmsDeliveryState>>? GetDeliveryStateDelegate { get; set; }

    public Message? LastEmailMessage { get; private set; }
    public Message? LastSmsMessage { get; private set; }
    public Message? LastSmsCodeMessage { get; private set; }
    public long LastDeliveryStateMessageId { get; private set; }

    public Task SendEmail(Message message, CancellationToken cancellationToken = default)
    {
        LastEmailMessage = message;
        return SendEmailDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;
    }

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default)
    {
        LastSmsMessage = message;
        if (SendSmsDelegate is not null)
        {
            return SendSmsDelegate(message, cancellationToken);
        }

        return Task.FromResult(new SmsSendResult(
            "0",
            message.Recipient,
            0m,
            null,
            SubmittedAt: null,
            RelatedMessageIds: null,
            Metadata: null));
    }

    public Task SendSmsCode(Message message, CancellationToken cancellationToken = default)
    {
        LastSmsCodeMessage = message;
        return SendSmsCodeDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;
    }

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default)
    {
        LastDeliveryStateMessageId = messageId;
        if (GetDeliveryStateDelegate is not null)
        {
            return GetDeliveryStateDelegate(messageId, cancellationToken);
        }

        return Task.FromResult(new SmsDeliveryState(
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

/// <summary>
/// Fake implementation of <see cref="ISmsProvider"/> for testing purposes.
/// </summary>
internal sealed class FakeSmsProvider : ISmsProvider
{
    public Func<Message, CancellationToken, Task<SmsSendResult>>? SendSmsDelegate { get; set; }
    public Func<Message, CancellationToken, Task>? SendCodeDelegate { get; set; }
    public Func<long, CancellationToken, Task<SmsDeliveryState>>? GetDeliveryStateDelegate { get; set; }

    public Message? LastSmsMessage { get; private set; }
    public Message? LastCodeMessage { get; private set; }
    public long LastDeliveryStateMessageId { get; private set; }

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default)
    {
        LastSmsMessage = message;
        if (SendSmsDelegate is not null)
        {
            return SendSmsDelegate(message, cancellationToken);
        }

        return Task.FromResult(new SmsSendResult(
            "0",
            message.Recipient,
            0m,
            null,
            SubmittedAt: null,
            RelatedMessageIds: null,
            Metadata: null));
    }

    public Task SendCode(Message message, CancellationToken cancellationToken = default)
    {
        LastCodeMessage = message;
        return SendCodeDelegate?.Invoke(message, cancellationToken) ?? Task.CompletedTask;
    }

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default)
    {
        LastDeliveryStateMessageId = messageId;
        if (GetDeliveryStateDelegate is not null)
        {
            return GetDeliveryStateDelegate(messageId, cancellationToken);
        }

        return Task.FromResult(new SmsDeliveryState(
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
