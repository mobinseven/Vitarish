using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Communications.Domain.Models;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

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
                provider.GetRequiredService<IRequestContext>(),
                provider.GetRequiredService<IPartiesService>(),
                provider.GetRequiredService<IProjectsService>(),
                provider.GetRequiredService<IProceduresService>(),
                provider.GetRequiredService<IEventsService>(),
                provider.GetRequiredService<IRemindersService>(),
                provider.GetRequiredService<IMessagesService>(),
                provider.GetRequiredService<INotificationTemplateResolver>(),
                provider.GetRequiredService<IUsersService>());

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
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        IReadOnlyList<ReminderReadDto> due = await tenantContext.RemindersService.GetDueAsync(now, shutdownToken);

        foreach (ReminderReadDto reminder in due)
        {
            await ProcessReminderAsync(tenantContext, reminder, now, shutdownToken);
        }
    }

    private async Task ProcessReminderAsync(
        TenantReminderContext tenantContext,
        ReminderReadDto reminder,
        DateTime now,
        CancellationToken cancellationToken)
    {
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

        try
        {
        if (!string.IsNullOrWhiteSpace(reminder.TemplatePurpose))
        {
            await ProcessAgentTemplateReminderAsync(tenantContext, reminder, now, cancellationToken);
            return;
        }

        // Pre-rendered reminders (e.g. from session reminder policies) carry fully rendered
        // subject and body so they can be sent directly without looking up the Sea source entity.
        if (!string.IsNullOrWhiteSpace(reminder.TextBody))
        {
            await SendPreRenderedReminderAsync(tenantContext, reminder, now, cancellationToken);
            return;
        }

        TenantReadDto? tenant = await tenantContext.PartiesService.GetTenantAsync(Guid.Parse(tenantContext.RequestContext.TenantId!), cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} not found for Sea reminder {ReminderId}. Configuration failure — will retry.", tenantContext.RequestContext.TenantId, reminder.Id);
            return;
        }

        EventReadDto? evnt = await tenantContext.EventsService.GetAsync(reminder.SourceId, cancellationToken).GetValueOrDefaultAsync();
        if (evnt is null)
        {
            _logger.LogWarning("Event {EventId} not found for Sea reminder {ReminderId}. Configuration failure — will retry.", reminder.SourceId, reminder.Id);
            return;
        }

        ProjectReadDto? proj = await tenantContext.ProjectsService.GetAsync(evnt.ProjectId, cancellationToken).GetValueOrDefaultAsync();
        if (proj is null)
        {
            _logger.LogWarning("Project {ProjectId} not found for Sea reminder {ReminderId}. Configuration failure — will retry.", evnt.ProjectId, reminder.Id);
            return;
        }

        ProcedureReadDto? proc = await tenantContext.ProceduresService.GetAsync(evnt.ProcedureId, cancellationToken).GetValueOrDefaultAsync();
        if (proc is null)
        {
            _logger.LogWarning("Procedure {ProcedureId} not found for Sea reminder {ReminderId}. Configuration failure — will retry.", evnt.ProcedureId, reminder.Id);
            return;
        }

        string? recipientLocale = reminder.RecipientUserId is not null
            ? await GetUserLocaleAsync(tenantContext, reminder.RecipientUserId, cancellationToken)
            : null;

        ResolvedNotificationTemplate? resolvedTemplate = await tenantContext.TemplateResolver.ResolveAsync(
            recipientLocale,
            NotificationPurpose.SeaReminder,
            cancellationToken);

        string? providerMessageId = null;
        switch (reminder.Kind)
        {
            case ReminderKind.Email:
                if (resolvedTemplate is null || resolvedTemplate.EmailSubject is null || resolvedTemplate.EmailTextBody is null)
                {
                    _logger.LogWarning("No email template resolved for Sea reminder {ReminderId}. Configuration failure — will retry.", reminder.Id);
                    return;
                }

                string subject = resolvedTemplate.EmailSubject
                    .Replace("{EventTitle}", evnt.Title, StringComparison.Ordinal)
                    .Replace("{ProcedureName}", proc.Name, StringComparison.Ordinal)
                    .Replace("{ProjectName}", proj.Name, StringComparison.Ordinal);
                string body = resolvedTemplate.EmailTextBody
                    .Replace("{EventTitle}", evnt.Title, StringComparison.Ordinal)
                    .Replace("{ProcedureName}", proc.Name, StringComparison.Ordinal)
                    .Replace("{ProjectName}", proj.Name, StringComparison.Ordinal);
                string htmlBody = resolvedTemplate.EmailHtmlBody?
                    .Replace("{EventTitle}", System.Net.WebUtility.HtmlEncode(evnt.Title), StringComparison.Ordinal)
                    .Replace("{ProcedureName}", System.Net.WebUtility.HtmlEncode(proc.Name), StringComparison.Ordinal)
                    .Replace("{ProjectName}", System.Net.WebUtility.HtmlEncode(proj.Name), StringComparison.Ordinal)
                    ?? $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>";

                await tenantContext.MessagesService.SendEmail(new Message
                {
                    Recipient = reminder.Recipient,
                    Subject = subject,
                    Body = body,
                    HtmlBody = htmlBody,
                    Sender = null
                }, cancellationToken);
                break;

            case ReminderKind.Sms:
                if (resolvedTemplate is null || resolvedTemplate.SmsTemplateId is null)
                {
                    _logger.LogWarning("No SMS template resolved for Sea reminder {ReminderId}. Configuration failure — will retry.", reminder.Id);
                    return;
                }

                Dictionary<string, string> smsParameters = new()
                {
                    ["PROJECT"] = proj.Name,
                    ["PROCEDURE"] = proc.Name,
                    ["EVENT"] = evnt.Title,
                    ["TENANT"] = tenant.Name
                };

                SmsSendResult sent = await tenantContext.MessagesService.SendTemplatedSms(
                    reminder.Recipient,
                    resolvedTemplate.SmsTemplateId,
                    smsParameters,
                    cancellationToken);
                providerMessageId = sent.MessageId;
                break;
        }

        await tenantContext.RemindersService.RecordDeliveryAttemptAsync(reminder.Id, reminder.Kind, now, true, providerMessageId, null, cancellationToken);
        await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await tenantContext.RemindersService.RecordDeliveryAttemptAsync(reminder.Id, reminder.Kind, now, false, null, ex.Message, cancellationToken);
            _logger.LogWarning(ex, "Reminder {ReminderId} delivery failed; it remains pending for retry.", reminder.Id);
        }
    }

    private async Task ProcessAgentTemplateReminderAsync(
        TenantReminderContext tenantContext,
        ReminderReadDto reminder,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(reminder.TemplatePurpose, true, out NotificationPurpose purpose) || purpose is NotificationPurpose.VerificationCode)
            throw new InvalidOperationException($"Notification template purpose '{reminder.TemplatePurpose}' is not approved for reminders.");

        Dictionary<string, string> variables = string.IsNullOrWhiteSpace(reminder.TemplateVariablesJson)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(reminder.TemplateVariablesJson) ?? throw new InvalidOperationException("Reminder template variables are invalid.");
        string? locale = reminder.RecipientUserId is not null
            ? await GetUserLocaleAsync(tenantContext, reminder.RecipientUserId, cancellationToken)
            : null;
        ResolvedNotificationTemplate template = await tenantContext.TemplateResolver.ResolveAsync(locale, purpose, cancellationToken)
            ?? throw new InvalidOperationException($"No notification template resolved for reminder {reminder.Id}.");
        string Replace(string value) => variables.Aggregate(value, (current, pair) => current.Replace("{" + pair.Key + "}", pair.Value, StringComparison.Ordinal));
        string? providerMessageId = null;

        switch (reminder.Kind)
        {
            case ReminderKind.Email when template.EmailSubject is not null && template.EmailTextBody is not null:
                await tenantContext.MessagesService.SendEmail(new Message
                {
                    Recipient = reminder.Recipient,
                    Subject = Replace(template.EmailSubject),
                    Body = Replace(template.EmailTextBody),
                    HtmlBody = Replace(template.EmailHtmlBody ?? $"<p>{System.Net.WebUtility.HtmlEncode(template.EmailTextBody)}</p>"),
                    Sender = null
                }, cancellationToken);
                break;
            case ReminderKind.Sms when template.SmsTemplateId is not null:
                SmsSendResult sms = await tenantContext.MessagesService.SendTemplatedSms(reminder.Recipient, template.SmsTemplateId, variables, cancellationToken);
                providerMessageId = sms.MessageId;
                break;
            default:
                throw new InvalidOperationException($"The selected notification template does not support {reminder.Kind} reminders.");
        }

        await tenantContext.RemindersService.RecordDeliveryAttemptAsync(reminder.Id, reminder.Kind, now, true, providerMessageId, null, cancellationToken);
        await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
    }

    private async Task SendPreRenderedReminderAsync(
        TenantReminderContext tenantContext,
        ReminderReadDto reminder,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            string? providerMessageId = null;
            switch (reminder.Kind)
            {
                case ReminderKind.Email:
                    await tenantContext.MessagesService.SendEmail(new Message { Recipient = reminder.Recipient, Subject = reminder.Subject, Body = reminder.TextBody, HtmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(reminder.TextBody).Replace("\n", "<br>", StringComparison.Ordinal)}</p>", Sender = null }, cancellationToken);
                    break;
                case ReminderKind.Sms:
                    SmsSendResult sent = await tenantContext.MessagesService.SendSms(new Message { Recipient = reminder.Recipient, Body = reminder.TextBody }, cancellationToken);
                    providerMessageId = sent.MessageId;
                    break;
            }
            await tenantContext.RemindersService.RecordDeliveryAttemptAsync(reminder.Id, reminder.Kind, now, true, providerMessageId, null, cancellationToken);
            await tenantContext.RemindersService.MarkSentAsync(reminder.Id, now, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            await tenantContext.RemindersService.RecordDeliveryAttemptAsync(reminder.Id, reminder.Kind, now, false, null, ex.Message, cancellationToken);
            _logger.LogWarning(ex, "Pre-rendered reminder {ReminderId} delivery failed; it remains pending for retry.", reminder.Id);
        }
    }
    private async Task<string?> GetUserLocaleAsync(TenantReminderContext tenantContext, string userId, CancellationToken cancellationToken)
    {
        User? user = await tenantContext.UsersService.GetUserAsync(userId, cancellationToken);
        if (user?.ProfilePreferences is null) return null;
        return user.ProfilePreferences.TryGetValue("Culture", out string? culture) ? culture : null;
    }

    private readonly record struct TenantReminderContext(
        IRequestContext RequestContext,
        IPartiesService PartiesService,
        IProjectsService ProjectsService,
        IProceduresService ProceduresService,
        IEventsService EventsService,
        IRemindersService RemindersService,
        IMessagesService MessagesService,
        INotificationTemplateResolver TemplateResolver,
        IUsersService UsersService);
}
