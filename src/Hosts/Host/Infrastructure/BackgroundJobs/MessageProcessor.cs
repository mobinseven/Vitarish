using Vitarish.Communications.Application.Abstractions;
using CommunicationsMessage = Vitarish.Communications.Domain.Models.Message;

namespace Vitarish.Host.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes messages enqueued via IMessageQueue using Hangfire.
/// </summary>
public class MessageProcessor(ILogger<MessageProcessor> logger, IMessagesService messagesService)
{
    /// <summary>
    /// Processes a message by sending it via the appropriate channel.
    /// This method is invoked by Hangfire as a background job.
    /// </summary>
    public async Task ProcessMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        string descriptor = $"{nameof(MessageProcessor)}:{message.GetType().Name}";

        try
        {
            switch (message)
            {
                case EmailMessage email:
                    logger.LogInformation("Processing email message to {MaskedTo}", MaskEmail(email.To));
                    await messagesService.SendEmail(new CommunicationsMessage
                    {
                        Recipient = email.To,
                        Subject = email.Subject,
                        Body = email.TextBody,
                        HtmlBody = email.HtmlBody
                    });
                    logger.LogInformation("Successfully sent email to {MaskedTo}", MaskEmail(email.To));
                    break;

                case SmsMessage sms:
                    logger.LogInformation("Processing SMS message to {MaskedPhoneNumber}", MaskPhoneNumber(sms.PhoneNumber));
                    await messagesService.SendSmsCode(new CommunicationsMessage
                    {
                        Recipient = sms.PhoneNumber,
                        Body = sms.Code
                    }, CancellationToken.None);
                    logger.LogInformation("Successfully sent SMS to {MaskedPhoneNumber}", MaskPhoneNumber(sms.PhoneNumber));
                    break;

                default:
                    logger.LogWarning("Unknown message type: {MessageType}", message.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing {Descriptor}", descriptor);
            throw; // Re-throw to let Hangfire handle retry logic
        }
    }

    /// <summary>
    /// Masks an email address to protect PII (e.g., "t***@example.com").
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        int atIndex = email.IndexOf('@');
        return atIndex <= 0 ? "***" : $"{email[0]}***{email[atIndex..]}";
    }

    /// <summary>
    /// Masks a phone number to protect PII (e.g., "+*******7890").
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return "***";
        }

        if (phoneNumber.Length <= 4)
        {
            return "***";
        }

        string prefix = phoneNumber.StartsWith("+") ? "+" : string.Empty;
        return $"{prefix}*******{phoneNumber[^4..]}";
    }
}
