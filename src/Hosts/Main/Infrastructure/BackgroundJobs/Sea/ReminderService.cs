using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Extensions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

internal sealed class ReminderService(IServiceScopeFactory scopeFactory, ILogger<ReminderService> logger, TimeProvider? timeProvider = null) : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PeriodicTimer timer = new(TimeSpan.FromMinutes(10), _timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessRemindersAsync(stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        IRemindersService remindersService = scope.ServiceProvider.GetRequiredService<IRemindersService>();
        IEventsService eventsService = scope.ServiceProvider.GetRequiredService<IEventsService>();
        IMessagesService messageClient = scope.ServiceProvider.GetRequiredService<IMessagesService>();

        DateTimeOffset now = _timeProvider.GetUtcNow();

        IReadOnlyList<ReminderDto> due = await remindersService.GetDueAsync(now, cancellationToken);

        foreach (ReminderDto reminder in due)
        {
            try
            {
                await ProcessReminderAsync(reminder, eventsService, messageClient, remindersService, now, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process reminder '{ReminderId}'.", reminder.Id);
            }
        }
    }

    private async Task ProcessReminderAsync(
        ReminderDto reminder,
        IEventsService eventsService,
        IMessagesService messageClient,
        IRemindersService remindersService,
        DateTimeOffset now,
        CancellationToken cancellationToken)
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
            // TODO: This is temporary as our provider will not send sms in these hours. Also, checking delivery is not responsibility of this service. So, until managing this issue in CaaS side we will just avoid sending sms in these hours.
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

                    await messageClient.SendEmail(new Message { Recipient = reminder.Recipient, Subject = reminder.Subject, Body = reminder.TextBody, HtmlBody = htmlbody, Sender = null }, cancellationToken);
                    break;
                case ReminderKind.Sms:

                    _ = await messageClient.SendSms(new Message { Recipient = reminder.Recipient, Body = reminder.TextBody, Sender = null }, cancellationToken);
                    break;
            }

            await remindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
        }
    }
}

