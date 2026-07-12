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
        PlatformCommandResult result = await command.CreateAsync(model, ct);

        return result.Succeeded
            ? Results.Ok(PlatformCommandResponse.Success())
            : Results.BadRequest(PlatformCommandResponse.Failure(result.Error!));
    }

    private static async Task<IResult> UpdateAsync(
        string normalizedLocale,
        LocaleNotificationRecordRequest request,
        IPlatformLocaleNotificationCommand command,
        CancellationToken ct)
    {
        LocaleNotificationRecordWriteModel model = ToWriteModel(request);
        PlatformCommandResult result = await command.UpdateAsync(normalizedLocale, model, ct);

        return result.Succeeded
            ? Results.Ok(PlatformCommandResponse.Success())
            : result.Error?.StartsWith("Locale notification record") == true
                ? Results.NotFound(PlatformCommandResponse.Failure(result.Error!))
                : Results.BadRequest(PlatformCommandResponse.Failure(result.Error!));
    }

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
