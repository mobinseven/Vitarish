using Hangfire;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Extensions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Processes Sea reminders using Hangfire.
/// </summary>
public class ReminderProcessor(
    ILogger<ReminderProcessor> logger,
    IRemindersService remindersService,
    IEventsService eventsService,
    IMessagesService messageClient,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Processes due reminders. Intended to be invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessDueReminders(IJobCancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationToken shutdownToken = cancellationToken.ShutdownToken;

        DateTimeOffset now = _timeProvider.GetUtcNow();

        IReadOnlyList<ReminderDto> due = await remindersService.GetDueAsync(now, shutdownToken);

        foreach (ReminderDto reminder in due)
        {
            try
            {
                await ProcessReminderAsync(reminder, now, shutdownToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process reminder '{ReminderId}'.", reminder.Id);
            }
        }
    }

    private async Task ProcessReminderAsync(ReminderDto reminder, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EventDto? evt = await eventsService.GetAsync(reminder.EventId, cancellationToken).GetValueOrDefaultAsync();
        if (evt is null)
        {
            await remindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
            return;
        }

        string htmlbody = $"<p>{reminder.TextBody}</p>";

        bool shouldSkipQuietHours = false;
        if (reminder.Kind is ReminderKind.Sms)
        {
            int hour = _timeProvider.GetLocalNow().Hour;
            // TODO: This is temporary as our provider will not send sms in these hours. Also, checking delivery is not the
            // responsibility of this service. So, until managing this issue in CaaS side we will just avoid sending sms in these hours.
            if (hour is < 8 or > 22)
            {
                shouldSkipQuietHours = true;
            }
        }

        if (!shouldSkipQuietHours)
        {
            switch (reminder.Kind)
            {
                case ReminderKind.Email:

                    await messageClient.SendEmail(new Message
                    {
                        Recipient = reminder.Recipient,
                        Subject = reminder.Subject,
                        Body = reminder.TextBody,
                        HtmlBody = htmlbody,
                        Sender = null
                    }, cancellationToken);
                    break;
                case ReminderKind.Sms:

                    SmsSendResult smsResult = await messageClient.SendSms(new Message
                    {
                        Recipient = reminder.Recipient,
                        Body = reminder.TextBody,
                        Sender = null
                    }, cancellationToken);

                    if (string.IsNullOrWhiteSpace(smsResult.MessageId))
                    {
                        logger.LogError(
                            "SMS send returned no message ID for reminder '{ReminderId}' to '{Recipient}'.", 
                            reminder.Id,
                            reminder.Recipient);
                        return;
                    }

                    logger.LogInformation(
                        "Sent SMS reminder '{ReminderId}' to '{Recipient}' with message id '{MessageId}'.", 
                        reminder.Id,
                        reminder.Recipient,
                        smsResult.MessageId);

                    break;
            }

            await remindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
        }
    }
}
