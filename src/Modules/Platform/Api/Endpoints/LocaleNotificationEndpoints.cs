using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Contracts.Requests;
using Vitarish.Platform.Contracts.Responses;
using Vitarish.Platform.Contracts.Routes;

namespace Vitarish.Platform.Api.Endpoints;

public static class LocaleNotificationEndpoints
{
    public static RouteGroupBuilder MapLocaleNotificationEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet(PlatformApiRoutes.LocaleNotifications, ListAsync, "PlatformListLocaleNotifications", typeof(LocaleListItemResponse[]));
        group.MapModuleGet(PlatformApiRoutes.LocaleNotificationByLocale, GetAsync, "PlatformGetLocaleNotification", typeof(LocaleNotificationRecordResponse));
        group.MapModulePost(PlatformApiRoutes.LocaleNotifications, CreateAsync, "PlatformCreateLocaleNotification", typeof(PlatformCommandResponse));
        group.MapModulePut(PlatformApiRoutes.LocaleNotificationByLocale, UpdateAsync, "PlatformUpdateLocaleNotification", typeof(PlatformCommandResponse));
        return group;
    }

    private static async Task<IResult> ListAsync(
        IPlatformLocaleNotificationQuery query,
        CancellationToken ct)
    {
        IReadOnlyList<LocaleListItem> items = await query.ListAsync(ct);
        return Results.Ok(items.Select(i => new LocaleListItemResponse(i.NormalizedLocale, i.Locale)).ToArray());
    }

    private static async Task<IResult> GetAsync(
        string normalizedLocale,
        IPlatformLocaleNotificationQuery query,
        CancellationToken ct)
    {
        Domain.Entities.LocaleNotificationRecord? record = await query.GetAsync(normalizedLocale, ct);
        if (record is null) return Results.NotFound();

        return Results.Ok(ToResponse(record));
    }

    private static async Task<IResult> CreateAsync(
        LocaleNotificationRecordRequest request,
        IPlatformLocaleNotificationCommand command,
        CancellationToken ct)
    {
        LocaleNotificationRecordWriteModel model = ToWriteModel(request);
        LocaleNotificationCommandResult result = await command.CreateAsync(model, ct);

        return ToCommandResult(result);
    }

    private static async Task<IResult> UpdateAsync(
        string normalizedLocale,
        LocaleNotificationRecordRequest request,
        IPlatformLocaleNotificationCommand command,
        CancellationToken ct)
    {
        LocaleNotificationRecordWriteModel model = ToWriteModel(request);
        LocaleNotificationCommandResult result = await command.UpdateAsync(normalizedLocale, model, 1, ct);

        return ToCommandResult(result);
    }

    private static IResult ToCommandResult(LocaleNotificationCommandResult result)
        => result.Status switch
        {
            LocaleNotificationCommandStatus.Success => Results.Ok(PlatformCommandResponse.Success()),
            LocaleNotificationCommandStatus.NotFound => Results.NotFound(PlatformCommandResponse.Failure("Locale notification record was not found.")),
            LocaleNotificationCommandStatus.DuplicateLocale => Results.Conflict(PlatformCommandResponse.Failure("A record for this locale already exists.")),
            LocaleNotificationCommandStatus.StaleVersion => Results.Conflict(PlatformCommandResponse.Failure("Locale notification record was updated by another request.")),
            LocaleNotificationCommandStatus.Validation => Results.BadRequest(PlatformCommandResponse.Failure(result.ValidationFailure?.Rule.ToString() ?? "Validation")),
            _ => Results.BadRequest(PlatformCommandResponse.Failure("Validation"))
        };

    private static LocaleNotificationRecordResponse ToResponse(Domain.Entities.LocaleNotificationRecord record)
        => new(
            record.NormalizedLocale,
            record.Locale,
            record.VerificationSmsTemplateId,
            record.SeaReminderEmailSubject,
            record.SeaReminderEmailTextBody,
            record.SeaReminderEmailHtmlBody,
            record.SeaReminderSmsTemplateId,
            record.AppointmentReminderEmailSubject,
            record.AppointmentReminderEmailTextBody,
            record.AppointmentReminderEmailHtmlBody,
            record.AppointmentReminderSmsTemplateId);

    private static LocaleNotificationRecordWriteModel ToWriteModel(LocaleNotificationRecordRequest request)
        => new(
            request.Locale,
            request.VerificationSmsTemplateId,
            request.SeaReminderEmailSubject,
            request.SeaReminderEmailTextBody,
            request.SeaReminderEmailHtmlBody,
            request.SeaReminderSmsTemplateId,
            request.AppointmentReminderEmailSubject,
            request.AppointmentReminderEmailTextBody,
            request.AppointmentReminderEmailHtmlBody,
            request.AppointmentReminderSmsTemplateId);
}
