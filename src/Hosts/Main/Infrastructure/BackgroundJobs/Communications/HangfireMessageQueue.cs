using Hangfire;
using Vitarish.Communications.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Communications;

/// <summary>
/// Hangfire-backed implementation of ICommunicationCommandQueue that persists commands to storage.
/// </summary>
public class HangfireMessageQueue(IBackgroundJobClient backgroundJobClient) : ICommunicationCommandQueue
{
    public void Enqueue(CommunicationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Validate(command);
        _ = backgroundJobClient.Enqueue<MessageProcessor>(processor => processor.ProcessCommand(command));
    }

    private static void Validate(CommunicationCommand command)
    {
        switch (command)
        {
            case EmailCommunicationCommand email:
                ArgumentException.ThrowIfNullOrWhiteSpace(email.Recipient, nameof(email.Recipient));
                ArgumentException.ThrowIfNullOrWhiteSpace(email.Subject, nameof(email.Subject));
                ArgumentException.ThrowIfNullOrWhiteSpace(email.TextBody, nameof(email.TextBody));
                ArgumentException.ThrowIfNullOrWhiteSpace(email.HtmlBody, nameof(email.HtmlBody));
                break;
            case SmsCommunicationCommand sms:
                ArgumentException.ThrowIfNullOrWhiteSpace(sms.Recipient, nameof(sms.Recipient));
                ArgumentException.ThrowIfNullOrWhiteSpace(sms.Body, nameof(sms.Body));
                break;
            case SmsCodeCommunicationCommand smsCode:
                ArgumentException.ThrowIfNullOrWhiteSpace(smsCode.Recipient, nameof(smsCode.Recipient));
                ArgumentException.ThrowIfNullOrWhiteSpace(smsCode.Code, nameof(smsCode.Code));
                break;
            case SmsReminderCommunicationCommand reminder:
                ArgumentException.ThrowIfNullOrWhiteSpace(reminder.Recipient, nameof(reminder.Recipient));
                break;
            default:
                throw new NotSupportedException($"Unsupported communication command type '{command.GetType().Name}'.");
        }
    }
}
