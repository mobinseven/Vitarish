using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Processes Sea reminders using Hangfire.
/// </summary>
public class ReminderProcessor(
    ILogger<ReminderProcessor> logger,
    TenantBackgroundExecutor tenantBackgroundExecutor,
    TimeProvider? timeProvider = null)
{
    private const string BackgroundResource = "hangfire:sea:reminders";

    private readonly ILogger<ReminderProcessor> _logger = logger;
    private readonly TenantBackgroundExecutor _tenantBackgroundExecutor = tenantBackgroundExecutor;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Processes due reminders. Intended to be invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessDueReminders(CancellationToken cancellationToken = default)
    {
        async Task perTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
        {
            IRequestContext ctx = provider.GetRequiredService<IRequestContext>();
            TenantReminderContext tenantContext = new(
                provider.GetRequiredService<IRemindersService>(),
                provider.GetRequiredService<IMessagesService>());

            await ProcessTenantRemindersAsync(tenantContext, shutdownToken);
        }

        await _tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            perTenantAsync,
            cancellationToken);
    }

    private async Task ProcessTenantRemindersAsync(
        TenantReminderContext tenantContext,
        CancellationToken shutdownToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        IReadOnlyList<ReminderDto> due = await tenantContext.RemindersService.GetDueAsync(now, shutdownToken);

        foreach (ReminderDto reminder in due)
        {
            try
            {
                await ProcessReminderAsync(tenantContext, reminder, now, shutdownToken);
            }
            catch
            {
            }
        }
    }

    private async Task ProcessReminderAsync(
        TenantReminderContext tenantContext,
        ReminderDto reminder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        string htmlbody = $"<p>{System.Net.WebUtility.HtmlEncode(reminder.TextBody)}</p>";

        bool shouldSkipQuietHours = false;
        if (reminder.Kind is ReminderKind.Sms)
        {
            int hour = _timeProvider.GetLocalNow().Hour;
            // TODO: This is temporary as our provider will not send sms in these hours. Also, checking delivery is not the
            // responsibility of this service. So, until managing this issue in CaaS side we will just avoid sending sms in these hours.
            if (hour is >= 22 or < 8)
            {
                shouldSkipQuietHours = true;
            }
        }

        if (shouldSkipQuietHours)
        {
            _logger.LogInformation(
                "Deferring reminder '{ReminderId}' to '{Recipient}' due to quiet hours (10pm-8am).",
                reminder.Id,
                reminder.Kind == ReminderKind.Sms ? MaskPhoneNumber(reminder.Recipient) : reminder.Recipient);
            return;
        }

        switch (reminder.Kind)
        {
            case ReminderKind.Email:

                await tenantContext.MessagesService.SendEmail(new Message
                {
                    Recipient = reminder.Recipient,
                    Subject = reminder.Subject,
                    Body = reminder.TextBody,
                    HtmlBody = htmlbody,
                    Sender = null
                }, cancellationToken);
                break;
            case ReminderKind.Sms:

                SmsSendResult smsResult = await tenantContext.MessagesService.SendSms(new Message
                {
                    Recipient = reminder.Recipient,
                    Body = reminder.TextBody,
                    Sender = null
                }, cancellationToken);

                if (string.IsNullOrWhiteSpace(smsResult.MessageId))
                {
                    _logger.LogError(
                        "SMS send returned no message ID for reminder '{ReminderId}' to '{Recipient}'. Marking as sent to avoid retries.",
                        reminder.Id,
                        MaskPhoneNumber(reminder.Recipient));
                    await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
                    return;
                }

                _logger.LogInformation(
                    "Sent SMS reminder '{ReminderId}' to '{Recipient}' with message id '{MessageId}'.",
                    reminder.Id,
                    MaskPhoneNumber(reminder.Recipient),
                    smsResult.MessageId);

                break;
        }

        await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
    }

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

    private readonly record struct TenantReminderContext(
        IRemindersService RemindersService,
        IMessagesService MessagesService);
}
