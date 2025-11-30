using Vorn.Aaas.Interface.Infrastructure.Services;
using Vorn.Caas.Client.Abstractions;

namespace Vorn.Aaas.Host.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes messages enqueued via IMessageQueue using Hangfire.
/// </summary>
public class MessageProcessor(ILogger<MessageProcessor> logger, IMessageClient messageClient)
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
                    await messageClient.SendEmailAsync(email.To, email.Subject, email.TextBody, email.HtmlBody, cancellationToken: CancellationToken.None);
                    logger.LogInformation("Successfully sent email to {MaskedTo}", MaskEmail(email.To));
                    break;

                case SmsMessage sms:
                    logger.LogInformation("Processing SMS message to {MaskedPhoneNumber}", MaskPhoneNumber(sms.PhoneNumber));
                    await messageClient.SendSmsCodeAsync(sms.PhoneNumber, sms.Code, CancellationToken.None);
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
            return "***";

        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***";

        return $"{email[0]}***{email.Substring(atIndex)}";
    }

    /// <summary>
    /// Masks a phone number to protect PII (e.g., "+*******7890").
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "***";

        if (phoneNumber.Length <= 4)
            return "***";

        string prefix = phoneNumber.StartsWith("+") ? "+" : "";
        return $"{prefix}*******{phoneNumber.Substring(phoneNumber.Length - 4)}";
    }
}
