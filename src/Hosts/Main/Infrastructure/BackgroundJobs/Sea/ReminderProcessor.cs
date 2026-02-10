using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Parties.Application.Dtos;
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
                provider.GetRequiredService<IStringLocalizer>(),
                provider.GetRequiredService<IRequestContext>(),
                provider.GetRequiredService<IPartiesService>(),
                provider.GetRequiredService<IProjectsService>(),
                provider.GetRequiredService<IProceduresService>(),
                provider.GetRequiredService<IEventsService>(),
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
            await ProcessReminderAsync(tenantContext, reminder, now, shutdownToken);
        }
    }

    private async Task ProcessReminderAsync(
        TenantReminderContext tenantContext,
        ReminderDto reminder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        TenantReadDto? tenant = await tenantContext.PartiesService.GetTenantAsync(Guid.Parse(tenantContext.RequestContext.TenantId!), cancellationToken);
        if (tenant is null)
        {
            return;
        }

        EventDto? evnt = await tenantContext.EventsService.GetAsync(reminder.EventId, cancellationToken).GetValueOrDefaultAsync();
        if (evnt is null)
        {
            return;
        }

        ProjectDto? proj = await tenantContext.ProjectsService.GetAsync(evnt.ProjectId, cancellationToken).GetValueOrDefaultAsync();
        if (proj is null)
        {
            return;
        }

        ProcedureDto? proc = await tenantContext.ProceduresService.GetAsync(evnt.ProcedureId, cancellationToken).GetValueOrDefaultAsync();
        if (proc is null)
        {
            return;
        }


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
            return;
        }

        switch (reminder.Kind)
        {
            case ReminderKind.Email:
                string content = tenantContext.Localizer["ReminderText", evnt.Title, proc.Name, proj.Name];
                string htmlbody = $"<p>{System.Net.WebUtility.HtmlEncode(content)}</p>";
                await tenantContext.MessagesService.SendEmail(new Message
                {
                    Recipient = reminder.Recipient,
                    Subject = tenantContext.Localizer["Reminder"],
                    Body = content,
                    HtmlBody = htmlbody,
                    Sender = null
                }, cancellationToken);
                break;
            case ReminderKind.Sms:

                await tenantContext.MessagesService.SendSmsReminder(reminder.Recipient, proj.Name, proc.Name, evnt.Title, tenant.Name, cancellationToken);

                break;
        }

        await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
    }
    private readonly record struct TenantReminderContext(
        IStringLocalizer Localizer,
        IRequestContext RequestContext,
        IPartiesService PartiesService,
        IProjectsService ProjectsService,
        IProceduresService ProceduresService,
        IEventsService EventsService,
        IRemindersService RemindersService,
        IMessagesService MessagesService);
}
