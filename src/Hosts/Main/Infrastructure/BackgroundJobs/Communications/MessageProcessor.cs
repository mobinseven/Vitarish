using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Communications.Domain.Options;
using DeliveryMessage = Vitarish.Communications.Domain.Models.Message;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Communications;

/// <summary>
/// Processes communications commands enqueued via ICommunicationCommandQueue using Hangfire.
/// </summary>
public class MessageProcessor(ILogger<MessageProcessor> logger, IMessagesService messagesService, ICommunicationsModule communicationsModule, IOptionsMonitor<NotificationOptions>? options = null)
{
    public async Task ProcessSelfTests()
    {
        try
        {
            string recipient = options.CurrentValue.StartupSmsRecipient;

            DeliveryMessage msg = new()
            {
                Recipient = recipient,
                Body = "Startup health check SMS from Vorn Communications"
            };

            SmsSendResult result = await messagesService.SendSms(msg, CancellationToken.None);
            communicationsModule.IsAvailable = true;
        }
        catch
        {
            communicationsModule.IsAvailable = false;
        }
    }
    /// <summary>
    /// Processes a communications command by sending it via the appropriate channel.
    /// This method is invoked by Hangfire as a background job.
    /// </summary>
    public async Task ProcessCommand(CommunicationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        string descriptor = $"{nameof(MessageProcessor)}:{command.GetType().Name}";

        try
        {
            switch (command)
            {
                case EmailCommunicationCommand email:
                    logger.LogInformation("Processing email command to {MaskedTo}", MaskEmail(email.Recipient));
                    await messagesService.SendEmail(new DeliveryMessage
                    {
                        Recipient = email.Recipient,
                        Subject = email.Subject,
                        Body = email.TextBody,
                        HtmlBody = email.HtmlBody
                    }, CancellationToken.None);
                    logger.LogInformation("Successfully sent email to {MaskedTo}", MaskEmail(email.Recipient));
                    break;

                case SmsCommunicationCommand sms:
                    logger.LogInformation("Processing SMS command to {MaskedPhoneNumber}", MaskPhoneNumber(sms.Recipient));
                    await messagesService.SendSms(new DeliveryMessage
                    {
                        Recipient = sms.Recipient,
                        Body = sms.Body
                    }, CancellationToken.None);
                    logger.LogInformation("Successfully sent SMS to {MaskedPhoneNumber}", MaskPhoneNumber(sms.Recipient));
                    break;

                case SmsCodeCommunicationCommand smsCode:
                    logger.LogInformation("Processing SMS code command to {MaskedPhoneNumber}", MaskPhoneNumber(smsCode.Recipient));
                    await messagesService.SendSmsCode(new DeliveryMessage
                    {
                        Recipient = smsCode.Recipient,
                        Body = smsCode.Code
                    }, CancellationToken.None);
                    logger.LogInformation("Successfully sent SMS code to {MaskedPhoneNumber}", MaskPhoneNumber(smsCode.Recipient));
                    break;

                case SmsReminderCommunicationCommand reminder:
                    logger.LogInformation("Processing SMS reminder to {MaskedPhoneNumber}", MaskPhoneNumber(reminder.Recipient));
                    await messagesService.SendSmsReminder(
                        reminder.Recipient,
                        reminder.Project,
                        reminder.Procedure,
                        reminder.Event,
                        reminder.Tenant,
                        CancellationToken.None);
                    logger.LogInformation("Successfully sent SMS reminder to {MaskedPhoneNumber}", MaskPhoneNumber(reminder.Recipient));
                    break;

                default:
                    logger.LogWarning("Unsupported communication command type: {CommandType}", command.GetType().Name);
                    throw new NotSupportedException($"Unsupported communication command type '{command.GetType().Name}'.");
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
