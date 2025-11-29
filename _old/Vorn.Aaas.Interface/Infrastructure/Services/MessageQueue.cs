namespace Vorn.Aaas.Interface.Infrastructure.Services;

using System.Threading.Channels;

internal interface IMessageQueue
{
    void EnqueueEmail(string to, string subject, string textBody, string htmlBody);
    void EnqueueSms(string phoneNumber, string code);
    ValueTask<Message> DequeueAsync(CancellationToken ct);
}

internal abstract record Message;
internal record EmailMessage(string To, string Subject, string TextBody, string HtmlBody) : Message;
internal record SmsMessage(string PhoneNumber, string Code) : Message;

internal class MessageQueue : IMessageQueue
{
    private readonly Channel<Message> queue = Channel.CreateUnbounded<Message>();

    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody) =>
        queue.Writer.TryWrite(new EmailMessage(to, subject, textBody, htmlBody));

    public void EnqueueSms(string phoneNumber, string code) =>
        queue.Writer.TryWrite(new SmsMessage(phoneNumber, code));

    public ValueTask<Message> DequeueAsync(CancellationToken ct) => queue.Reader.ReadAsync(ct);
}
