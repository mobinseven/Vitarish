using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting;

internal static class SeaApiEndpoints
{
    public static void MapSeaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("SeaModule requires a WebApplication for endpoint configuration.");
        }

        RouteGroupBuilder group = app.MapGroup("/api/sea").WithTags("Sea");

        MapProjects(group);
        MapProcedures(group);
        MapEvents(group);
        MapAttachments(group);
        MapEventRoles(group);
        MapRoles(group);
        MapUserRoles(group);
        MapReminders(group);
        MapReceipts(group);
        MapGodView(group);
        MapUsageConfiguration(group);
        MapDatabaseStatistics(group);
    }

    private static void MapProjects(RouteGroupBuilder group)
    {
        _ = group.MapPost("/projects", async ([FromBody] AddProjectRequest request, IProjectsService service, CancellationToken ct) =>
        {
            Guid id = await service.AddAsync(request.Name, request.Description, request.Template, request.Metadata, ct);
            return Results.Ok(id);
        });

        _ = group.MapPut("/projects/{id:guid}", async (Guid id, [FromBody] UpdateProjectRequest request, IProjectsService service, CancellationToken ct) =>
        {
            await service.UpdateAsync(id, request.Name, request.Description, request.Template, request.Metadata, ct);
            return Results.NoContent();
        });

        _ = group.MapPatch("/projects/{id:guid}/metadata", async (Guid id, [FromBody] UpdateMetadataRequest request, IProjectsService service, CancellationToken ct) =>
        {
            await service.UpdateMetadataAsync(id, request.Metadata, ct);
            return Results.NoContent();
        });

        _ = group.MapGet("/projects/{id:guid}/snapshot", async (Guid id, IProjectsService service, CancellationToken ct) =>
        {
            ProjectSnapshot? snapshot = await service.CaptureSnapshotAsync(id, ct);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });
    }

    private static void MapProcedures(RouteGroupBuilder group)
    {
        _ = group.MapPost("/procedures", async ([FromBody] AddProcedureRequest request, IProceduresService service, CancellationToken ct) =>
        {
            Guid id = await service.AddAsync(request.ProjectId, request.Name, ct);
            return Results.Ok(id);
        });

        _ = group.MapPut("/procedures/{id:guid}", async (Guid id, [FromBody] UpdateProcedureRequest request, IProceduresService service, CancellationToken ct) =>
        {
            await service.UpdateAsync(id, request.Name, ct);
            return Results.NoContent();
        });

        _ = group.MapDelete("/procedures/{id:guid}", async (Guid id, IProceduresService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        _ = group.MapPatch("/procedures/{id:guid}/metadata", async (Guid id, [FromBody] UpdateMetadataRequest request, IProceduresService service, CancellationToken ct) =>
        {
            await service.UpdateMetadataAsync(id, request.Metadata, ct);
            return Results.NoContent();
        });

        _ = group.MapGet("/procedures/{id:guid}/snapshot", async (Guid id, IProceduresService service, CancellationToken ct) =>
        {
            ProcedureSnapshot? snapshot = await service.CaptureSnapshotAsync(id, ct);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });
    }

    private static void MapEvents(RouteGroupBuilder group)
    {
        _ = group.MapGet("/procedures/{procedureId:guid}/events", async (Guid procedureId, IEventsService service, CancellationToken ct) =>
        {
            IReadOnlyList<EventDto> items = await service.GetByProcedureAsync(procedureId, ct);
            return Results.Ok(items);
        });

        _ = group.MapPost("/events", async ([FromBody] AddEventRequest request, IEventsService service, CancellationToken ct) =>
        {
            EventDto? evt = await service.AddAsync(request.ProjectId, request.ProcedureId, request.PreviousEventId, ct);
            return evt is null ? Results.BadRequest() : Results.Ok(evt);
        });

        _ = group.MapPut("/events/{id:guid}", async (Guid id, [FromBody] UpdateEventRequest request, IEventsService service, CancellationToken ct) =>
        {
            EventDto? evt = await service.UpdateAsync(id, request.Title, request.Description, ct);
            return evt is null ? Results.NotFound() : Results.Ok(evt);
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/{eventId:guid}/move-up", async (Guid procedureId, Guid eventId, IEventsService service, CancellationToken ct) =>
        {
            bool moved = await service.MoveUpAsync(procedureId, eventId, ct);
            return moved ? Results.NoContent() : Results.NotFound();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/{eventId:guid}/move-down", async (Guid procedureId, Guid eventId, IEventsService service, CancellationToken ct) =>
        {
            bool moved = await service.MoveDownAsync(procedureId, eventId, ct);
            return moved ? Results.NoContent() : Results.NotFound();
        });

        _ = group.MapDelete("/procedures/{procedureId:guid}/events/{eventId:guid}", async (Guid procedureId, Guid eventId, IEventsService service, CancellationToken ct) =>
        {
            bool removed = await service.RemoveEventAsync(procedureId, eventId, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/repair", async (Guid procedureId, IEventsService service, CancellationToken ct) =>
        {
            await service.RepairChainAsync(procedureId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/chain", async (Guid procedureId, [FromBody] IEnumerable<EventChainChangeDto> chain, IEventsService service, CancellationToken ct) =>
        {
            await service.UpdateChainAsync(procedureId, chain, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/events/{eventId:guid}/commit", async (Guid eventId, [FromBody] CommitEventRequest request, IEventsService service, CancellationToken ct) =>
        {
            await service.CommitAsync(eventId, request.ReporterUserId, request.CommitTime, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/commit", async (Guid procedureId, [FromBody] CommitEventRequest request, IEventsService service, CancellationToken ct) =>
        {
            int count = await service.CommitAllByProcedureAsync(procedureId, request.ReporterUserId, request.CommitTime, ct);
            return Results.Ok(count);
        });

        _ = group.MapPost("/events/{eventId:guid}/uncommit", async (Guid eventId, IEventsService service, CancellationToken ct) =>
        {
            await service.UncommitAsync(eventId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/events/uncommit", async (Guid procedureId, IEventsService service, CancellationToken ct) =>
        {
            int count = await service.UncommitAllByProcedureAsync(procedureId, ct);
            return Results.Ok(count);
        });

        _ = group.MapGet("/events/{eventId:guid}/reminders/count", async (Guid eventId, IEventsService service, CancellationToken ct) =>
        {
            int count = await service.GetRemindersCountAsync(eventId, ct);
            return Results.Ok(count);
        });

        _ = group.MapGet("/events/{id:guid}/snapshot", async (Guid id, IEventsService service, CancellationToken ct) =>
        {
            EventSnapshot? snapshot = await service.CaptureSnapshotAsync(id, ct);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });
    }

    private static void MapAttachments(RouteGroupBuilder group)
    {
        _ = group.MapGet("/events/{eventId:guid}/attachments", async (Guid eventId, IAttachmentsService service, CancellationToken ct) =>
        {
            IReadOnlyList<AttachmentDto> attachments = await service.GetByEventAsync(eventId, ct);
            return Results.Ok(attachments);
        });

        _ = group.MapGet("/events/{eventId:guid}/attachments/count", async (Guid eventId, IAttachmentsService service, CancellationToken ct) =>
        {
            int count = await service.GetCountByEventAsync(eventId, ct);
            return Results.Ok(count);
        });

        _ = group.MapPost("/events/{eventId:guid}/attachments", async (Guid eventId, [FromBody] AddAttachmentRequest request, IAttachmentsService service, CancellationToken ct) =>
        {
            AttachmentDto? attachment = await service.AddAsync(eventId, request.Content, request.ContentType, request.ContentThumbnail, request.Size, ct);
            return attachment is null ? Results.BadRequest() : Results.Ok(attachment);
        });

        _ = group.MapPost("/events/{eventId:guid}/attachments/bulk", async (Guid eventId, [FromBody] IEnumerable<AttachmentDto> attachments, IAttachmentsService service, CancellationToken ct) =>
        {
            await service.AddAsync(eventId, attachments, ct);
            return Results.NoContent();
        });

        _ = group.MapDelete("/events/{eventId:guid}/attachments/{attachmentId:guid}", async (Guid eventId, Guid attachmentId, IAttachmentsService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(eventId, attachmentId, ct);
            return Results.NoContent();
        });

        _ = group.MapPut("/events/{eventId:guid}/attachments/{attachmentId:guid}", async (Guid eventId, Guid attachmentId, [FromBody] UpdateAttachmentRequest request, IAttachmentsService service, CancellationToken ct) =>
        {
            AttachmentDto? attachment = await service.UpdateAsync(eventId, attachmentId, request.Content, ct);
            return attachment is null ? Results.NotFound() : Results.Ok(attachment);
        });

        _ = group.MapGet("/attachments/{id:guid}/snapshot", async (Guid id, IAttachmentsService service, CancellationToken ct) =>
        {
            AttachmentSnapshot? snapshot = await service.CaptureSnapshotAsync(id, ct);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });
    }

    private static void MapEventRoles(RouteGroupBuilder group)
    {
        _ = group.MapGet("/events/{eventId:guid}/roles/ids", async (Guid eventId, IEventRolesService service, CancellationToken ct) =>
        {
            (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) ids = await service.GetIdsAsync(eventId, ct);
            return Results.Ok(ids);
        });

        _ = group.MapGet("/events/{eventId:guid}/roles/read", async (Guid eventId, IEventRolesService service, CancellationToken ct) =>
        {
            IReadOnlyList<Guid> ids = await service.GetReadIdsAsync(eventId, ct);
            return Results.Ok(ids);
        });

        _ = group.MapGet("/events/{eventId:guid}/roles/write", async (Guid eventId, IEventRolesService service, CancellationToken ct) =>
        {
            IReadOnlyList<Guid> ids = await service.GetWriteIdsAsync(eventId, ct);
            return Results.Ok(ids);
        });

        _ = group.MapPost("/events/{eventId:guid}/roles", async (Guid eventId, [FromBody] AssignEventRoleRequest request, IEventRolesService service, CancellationToken ct) =>
        {
            await service.AssignAsync(eventId, request.RoleId, request.IsWrite, ct);
            return Results.NoContent();
        });

        _ = group.MapDelete("/events/{eventId:guid}/roles", async (Guid eventId, [FromBody] AssignEventRoleRequest request, IEventRolesService service, CancellationToken ct) =>
        {
            await service.UnassignAsync(eventId, request.RoleId, request.IsWrite, ct);
            return Results.NoContent();
        });
    }

    private static void MapRoles(RouteGroupBuilder group)
    {
        _ = group.MapPost("/roles", async ([FromBody] AddRoleRequest request, IRolesService service, CancellationToken ct) =>
        {
            Guid id = await service.AddAsync(request.Name, ct);
            return Results.Ok(id);
        });

        _ = group.MapPut("/roles/{id:guid}", async (Guid id, [FromBody] AddRoleRequest request, IRolesService service, CancellationToken ct) =>
        {
            await service.UpdateAsync(id, request.Name, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/roles/paged", async ([FromBody] RoleDescriptorDto descriptor, IRolesService service, CancellationToken ct) =>
        {
            (IReadOnlyList<RoleDto> Items, int TotalItems) result = await service.GetPagedAsync(descriptor, ct);
            return Results.Ok(result);
        });
    }

    private static void MapUserRoles(RouteGroupBuilder group)
    {
        _ = group.MapPost("/user-roles", async ([FromBody] AssignUserRoleRequest request, IUserRolesService service, CancellationToken ct) =>
        {
            await service.AssignAsync(request.UserId, request.RoleId, request.Type, ct);
            return Results.NoContent();
        });

        _ = group.MapDelete("/user-roles", async ([FromBody] AssignUserRoleRequest request, IUserRolesService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(request.UserId, request.RoleId, ct);
            return Results.NoContent();
        });
    }

    private static void MapReminders(RouteGroupBuilder group)
    {
        _ = group.MapPost("/events/{eventId:guid}/reminders", async (Guid eventId, [FromBody] AddReminderRangeRequest request, IRemindersService service, CancellationToken ct) =>
        {
            IReadOnlyList<ReminderDto> reminders = await service.AddRangeAsync(eventId, request.Recipients, request.Subject, request.TextBody, request.RemindAt, ct);
            return Results.Ok(reminders);
        });

        _ = group.MapPut("/reminders/{id:guid}", async (Guid id, [FromBody] UpdateReminderRequest request, IRemindersService service, CancellationToken ct) =>
        {
            ReminderDto? reminder = await service.UpdateAsync(id, request.Kind, request.RecipientUserId, request.Recipient, request.Subject, request.TextBody, request.RemindAt, ct);
            return reminder is null ? Results.NotFound() : Results.Ok(reminder);
        });

        _ = group.MapGet("/events/{eventId:guid}/reminders", async (Guid eventId, IRemindersService service, CancellationToken ct) =>
        {
            IReadOnlyList<ReminderDto> reminders = await service.GetByEventAsync(eventId, ct);
            return Results.Ok(reminders);
        });

        _ = group.MapGet("/reminders/due", async ([FromQuery] DateTimeOffset now, IRemindersService service, CancellationToken ct) =>
        {
            IReadOnlyList<ReminderDto> reminders = await service.GetDueAsync(now, ct);
            return Results.Ok(reminders);
        });

        _ = group.MapPost("/reminders/{id:guid}/sent", async (Guid id, [FromBody] MarkReminderSentRequest request, IRemindersService service, CancellationToken ct) =>
        {
            await service.MarkSentAsync(id, request.SentAt, ct);
            return Results.NoContent();
        });
    }

    private static void MapReceipts(RouteGroupBuilder group)
    {
        _ = group.MapPost("/receipts", async ([FromBody] AddReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            Guid id = await service.AddAsync(request.UserId, request.ObjectId, ct);
            return Results.Ok(id);
        });

        _ = group.MapGet("/receipts/{objectId:guid}/readers", async (Guid objectId, IReceiptsService service, CancellationToken ct) =>
        {
            IReadOnlyList<Guid> readers = await service.GetReadersAsync(objectId, ct);
            return Results.Ok(readers);
        });

        _ = group.MapPost("/projects/{projectId:guid}/receipts/read", async (Guid projectId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkProjectReadAsync(request.UserId, projectId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/projects/{projectId:guid}/receipts/unread", async (Guid projectId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkProjectUnreadAsync(request.UserId, projectId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/receipts/read", async (Guid procedureId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkProcedureReadAsync(request.UserId, procedureId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/procedures/{procedureId:guid}/receipts/unread", async (Guid procedureId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkProcedureUnreadAsync(request.UserId, procedureId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/events/{eventId:guid}/receipts/read", async (Guid eventId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkEventReadAsync(request.UserId, eventId, ct);
            return Results.NoContent();
        });

        _ = group.MapPost("/events/{eventId:guid}/receipts/unread", async (Guid eventId, [FromBody] SetReceiptRequest request, IReceiptsService service, CancellationToken ct) =>
        {
            await service.MarkEventUnreadAsync(request.UserId, eventId, ct);
            return Results.NoContent();
        });

        _ = group.MapGet("/receipts/{userId:guid}/{objectId:guid}", async (Guid userId, Guid objectId, IReceiptsService service, CancellationToken ct) =>
        {
            bool hasReceipt = await service.GetAsync(userId, objectId, ct);
            return Results.Ok(hasReceipt);
        });
    }

    private static void MapGodView(RouteGroupBuilder group)
    {
        _ = group.MapPost("/godview/projects", async ([FromBody] GodViewPageRequest request, IGodViewService service, CancellationToken ct) =>
        {
            GodViewPage page = await service.GetProjectsAsync(request, ct);
            return Results.Ok(page);
        });

        _ = group.MapPost("/godview/procedures", async ([FromBody] GodViewPageRequest request, IGodViewService service, CancellationToken ct) =>
        {
            GodViewPage page = await service.GetProceduresAsync(request, ct);
            return Results.Ok(page);
        });

        _ = group.MapPost("/godview/events", async ([FromBody] GodViewPageRequest request, IGodViewService service, CancellationToken ct) =>
        {
            GodViewPage page = await service.GetEventsAsync(request, ct);
            return Results.Ok(page);
        });

        _ = group.MapPost("/godview/attachments", async ([FromBody] GodViewPageRequest request, IGodViewService service, CancellationToken ct) =>
        {
            GodViewPage page = await service.GetAttachmentsAsync(request, ct);
            return Results.Ok(page);
        });

        _ = group.MapPost("/godview/reminders", async ([FromBody] GodViewPageRequest request, IGodViewService service, CancellationToken ct) =>
        {
            GodViewPage page = await service.GetRemindersAsync(request, ct);
            return Results.Ok(page);
        });
    }

    private static void MapUsageConfiguration(RouteGroupBuilder group)
    {
        _ = group.MapGet("/usage-configuration", async (IUsageConfigurationService service, CancellationToken ct) =>
        {
            UsageConfigurationDto? config = await service.GetAsync(ct);
            return config is null ? Results.NotFound() : Results.Ok(config);
        });

        _ = group.MapPut("/usage-configuration", async ([FromBody] UsageConfigurationDto dto, IUsageConfigurationService service, CancellationToken ct) =>
        {
            UsageConfigurationDto? config = await service.UpdateAsync(dto, ct);
            return config is null ? Results.BadRequest() : Results.Ok(config);
        });

        _ = group.MapGet("/usage-configuration/raw", async (IUsageConfigurationRepository repo, CancellationToken ct) =>
        {
            UsageConfiguration? config = await repo.GetAsync(ct);
            return config is null ? Results.NotFound() : Results.Ok(config);
        });

        _ = group.MapPut("/usage-configuration/raw", async ([FromBody] UsageConfiguration entity, IUsageConfigurationRepository repo, CancellationToken ct) =>
        {
            UsageConfiguration? config = await repo.UpdateAsync(entity, ct);
            return config is null ? Results.BadRequest() : Results.Ok(config);
        });
    }

    private static void MapDatabaseStatistics(RouteGroupBuilder group)
    {
        _ = group.MapGet("/database/statistics", async (IDatabaseStatisticsService service, CancellationToken ct) =>
        {
            DatabaseStatisticsDto stats = await service.GetAsync(ct);
            return Results.Ok(stats);
        });
    }

    private sealed record AddProjectRequest(string Name, string Description, bool Template, string Metadata);
    private sealed record UpdateProjectRequest(string Name, string Description, bool Template, string Metadata);
    private sealed record UpdateMetadataRequest(string Metadata);
    private sealed record AddProcedureRequest(Guid ProjectId, string Name);
    private sealed record UpdateProcedureRequest(string Name);
    private sealed record AddEventRequest(Guid ProjectId, Guid ProcedureId, Guid? PreviousEventId);
    private sealed record UpdateEventRequest(string Title, string Description);
    private sealed record CommitEventRequest(Guid ReporterUserId, DateTimeOffset CommitTime);
    private sealed record AddAttachmentRequest(string Content, string ContentType, string? ContentThumbnail, long Size);
    private sealed record UpdateAttachmentRequest(string Content);
    private sealed record AssignEventRoleRequest(Guid RoleId, bool IsWrite);
    private sealed record AddRoleRequest(string Name);
    private sealed record AssignUserRoleRequest(Guid UserId, Guid RoleId, RoleType Type);
    private sealed record AddReminderRangeRequest(IReadOnlyList<ReminderRecipientDto> Recipients, string Subject, string TextBody, DateTimeOffset RemindAt);
    private sealed record UpdateReminderRequest(ReminderKind Kind, string RecipientUserId, string Recipient, string Subject, string TextBody, DateTimeOffset RemindAt);
    private sealed record MarkReminderSentRequest(DateTimeOffset SentAt);
    private sealed record AddReceiptRequest(Guid UserId, Guid ObjectId);
    private sealed record SetReceiptRequest(Guid UserId);
}
