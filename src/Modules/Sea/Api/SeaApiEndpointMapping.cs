using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Contracts.Errors;
using Vitarish.Sea.Contracts.Results;
using Vitarish.Sea.Contracts.Routes;
using C = Vitarish.Sea.Contracts.Responses;
using CD = Vitarish.Sea.Contracts.Enums;
using D = Vitarish.Sea.Domain.Dto;
using DD = Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Api;

public static class SeaApiEndpointMapping
{
    public static IEndpointRouteBuilder MapSeaApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(SeaApiRoutes.Base);
        _ = group.MapModuleGet(SeaApiRoutes.Health, (CancellationToken cancellationToken) => Results.Ok(new SeaCommandResult(true)), "SeaGetHealth", typeof(SeaCommandResult));

        _ = group.MapModuleGet(SeaApiRoutes.Projects, async (IProjectsService s, CancellationToken ct) => OkList<C.ProjectReadDto>((await s.GetListAsync(new D.ProjectDescriptorDto(), ct)).Value ?? []), "SeaListProjects", typeof(C.ProjectReadDto[]));
        _ = group.MapModuleGet("/projects/{id:guid}", async (Guid id, IProjectsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.ProjectReadDto>), "SeaGetProject", typeof(C.ProjectReadDto));
        _ = group.MapModulePost(SeaApiRoutes.Projects, async (C.ProjectWriteDto r, IProjectsService s, CancellationToken ct) => Command(await s.AddAsync(ToDomain<D.ProjectWriteDto>(r), ct)), "SeaCreateProject", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Projects, async (C.ProjectWriteDto r, IProjectsService s, CancellationToken ct) => { await s.UpdateAsync(ToDomain<D.ProjectWriteDto>(r), ct); return Results.Ok(new SeaCommandResult(true, r.Id)); }, "SeaUpdateProject", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/projects/{id:guid}", async (Guid id, IProjectsService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaDeleteProject", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet("/projects/{id:guid}/snapshot", async (Guid id, IProjectsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, p => ToContract<C.ProjectSnapshot>(p)), "SeaGetProjectSnapshot", typeof(C.ProjectSnapshot));
        _ = group.MapModulePost("/projects/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProjectReadAsync(u,id,t), c, ct, id), "SeaMarkProjectRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/projects/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProjectUnreadAsync(u,id,t), c, ct, id), "SeaMarkProjectUnread", typeof(SeaCommandResult));

        _ = group.MapModuleGet("/projects/{projectId:guid}/procedures", async (Guid projectId, IProceduresService s, CancellationToken ct) => OkList<C.ProcedureReadDto>((await s.GetListAsync(new D.ProcedureDescriptorDto { ProjectId = projectId }, ct)).Value ?? []), "SeaListProcedures", typeof(C.ProcedureReadDto[]));
        _ = group.MapModuleGet("/procedures/{id:guid}", async (Guid id, IProceduresService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.ProcedureReadDto>), "SeaGetProcedure", typeof(C.ProcedureReadDto));
        _ = group.MapModulePost(SeaApiRoutes.Procedures, async (C.ProcedureWriteDto r, IProceduresService s, CancellationToken ct) => Command(await s.AddAsync(ToDomain<D.ProcedureWriteDto>(r), ct)), "SeaCreateProcedure", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Procedures, async (C.ProcedureWriteDto r, IProceduresService s, CancellationToken ct) => { await s.UpdateAsync(ToDomain<D.ProcedureWriteDto>(r), ct); return Results.Ok(new SeaCommandResult(true, r.Id)); }, "SeaUpdateProcedure", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/procedures/{id:guid}", async (Guid id, IProceduresService s, CancellationToken ct) => { await s.DeleteAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaDeleteProcedure", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModulePost("/procedures/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProcedureReadAsync(u,id,t), c, ct, id), "SeaMarkProcedureRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/procedures/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProcedureUnreadAsync(u,id,t), c, ct, id), "SeaMarkProcedureUnread", typeof(SeaCommandResult));

        _ = group.MapModuleGet("/procedures/{procedureId:guid}/events", async (Guid procedureId, IEventsService s, CancellationToken ct) => OkList<C.EventReadDto>(await s.GetByProcedureAsync(procedureId, ct)), "SeaListEvents", typeof(C.EventReadDto[]));
        _ = group.MapModuleGet("/events/{id:guid}", async (Guid id, IEventsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.EventReadDto>), "SeaGetEvent", typeof(C.EventReadDto));
        _ = group.MapModuleGet("/events/{id:guid}/permissions/write", async (Guid id, IAccessControlledEventsService s) => Results.Ok(await s.CanWriteEvent(id)), "SeaGetEventWritePermission", typeof(bool));
        _ = group.MapModulePost("/events/after/{previousEventId:guid}", async (Guid previousEventId, C.EventWriteDto r, IEventsService s, CancellationToken ct) => Command(await s.AddAsync(r.ProjectId, r.ProcedureId, previousEventId, ct)), "SeaCreateEventAfter", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/after", async (C.EventWriteDto r, IEventsService s, CancellationToken ct) => Command(await s.AddAsync(r.ProjectId, r.ProcedureId, null, ct)), "SeaCreateFirstEvent", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Events, async (C.EventWriteDto r, IEventsService s, CancellationToken ct) => Command(await s.UpdateAsync(r.Id, r.Title ?? string.Empty, r.Description ?? string.Empty, ct)), "SeaUpdateEvent", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/events/{id:guid}", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.RemoveEventAsync(Guid.Empty, id, ct), id)), "SeaDeleteEvent", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModulePost("/events/{id:guid}/move-up", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.MoveUpAsync(Guid.Empty, id, ct), id)), "SeaMoveEventUp", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/move-down", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.MoveDownAsync(Guid.Empty, id, ct), id)), "SeaMoveEventDown", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/commit", async (Guid id, IEventsService s, IRequestContext c, CancellationToken ct) => { await s.CommitAsync(id, UserGuid(c), DateTime.UtcNow, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaCommitEvent", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/uncommit", async (Guid id, IEventsService s, CancellationToken ct) => { await s.UncommitAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaUncommitEvent", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkEventReadAsync(u,id,t), c, ct, id), "SeaMarkEventRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkEventUnreadAsync(u,id,t), c, ct, id), "SeaMarkEventUnread", typeof(SeaCommandResult));

        _ = group.MapModuleGet("/events/{eventId:guid}/attachments", async (Guid eventId, IAttachmentsService s, CancellationToken ct) => OkList<C.AttachmentReadDto>(await s.GetByEventAsync(eventId, ct)), "SeaListAttachments", typeof(C.AttachmentReadDto[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/attachments/count", async (Guid eventId, IAttachmentsService s, CancellationToken ct) => Results.Ok(await s.GetCountByEventAsync(eventId, ct)), "SeaGetAttachmentCount", typeof(int));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/text", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.AddAsync(r.EventId, r.Content, r.ContentType, r.ContentThumbnail, r.Size, ct)), "SeaCreateTextAttachment", typeof(SeaCommandResult));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/file", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.AddAsync(r.EventId, r.Content, r.ContentType, r.ContentThumbnail, r.Size, ct)), "SeaCreateFileAttachment", typeof(SeaCommandResult));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/batch", async (IReadOnlyList<C.AttachmentWriteDto> r, IAttachmentsService s, CancellationToken ct) => { foreach (var a in r) await s.AddAsync(a.EventId, a.Content, a.ContentType, a.ContentThumbnail, a.Size, ct); return Results.Ok(new SeaCommandResult(true)); }, "SeaCreateAttachmentBatch", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Attachments + "/text", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.UpdateAsync(r.EventId, r.Id, r.Content, ct)), "SeaUpdateTextAttachment", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/attachments/{id:guid}", async (Guid id, IAttachmentsService s, CancellationToken ct) => { await s.RemoveAsync(Guid.Empty, id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaDeleteAttachment", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);

        _ = group.MapModuleGet(SeaApiRoutes.Roles, async (IRolesService s, CancellationToken ct) => OkList<C.RoleReadDto>((await s.GetListAsync(new D.RoleDescriptorDto(), ct)).Value ?? []), "SeaListRoles", typeof(C.RoleReadDto[]));
        _ = group.MapModulePost(SeaApiRoutes.Roles, async (C.RoleWriteDto r, IRolesService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(true, await s.AddAsync(r.Name, ct))), "SeaCreateRole", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Roles, async (C.RoleWriteDto r, IRolesService s, CancellationToken ct) => { await s.UpdateAsync(r.Id, r.Name, ct); return Results.Ok(new SeaCommandResult(true, r.Id)); }, "SeaUpdateRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/roles/{id:guid}", async (Guid id, IRolesService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaDeleteRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet("/users/{userId}/roles", async (string userId, IUserRolesService s, CancellationToken ct) => OkList<C.UserRoleReadDto>((await s.GetListAsync(new D.UserRoleDescriptorDto { UserId = Guid.Parse(userId) }, ct)).Value ?? []), "SeaListUserRoles", typeof(C.UserRoleReadDto[]));
        _ = group.MapModulePost(SeaApiRoutes.UserRoles, async (C.UserRoleWriteDto r, IUserRolesService s, CancellationToken ct) => { await s.AssignAsync(r.Id, r.RoleId, (DD.RoleType)(int)r.Type, ct); return Results.Ok(new SeaCommandResult(true)); }, "SeaAssignUserRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/user-roles/{id:guid}", async (Guid id, IUserRolesService s, IRequestContext c, CancellationToken ct) => { await s.RemoveAsync(UserGuid(c), id, ct); return Results.Ok(new SeaCommandResult(true)); }, "SeaRemoveUserRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);

        _ = group.MapModuleGet("/events/{eventId:guid}/roles", async (Guid eventId, IEventRolesService s, CancellationToken ct) => { var ids = await s.GetIdsAsync(eventId, ct); return Results.Ok(ids.ReadRoleIds.Concat(ids.WriteRoleIds).Distinct().ToArray()); }, "SeaListEventRoles", typeof(Guid[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/roles/read", async (Guid eventId, IEventRolesService s, CancellationToken ct) => Results.Ok(await s.GetReadIdsAsync(eventId, ct)), "SeaListEventReadRoles", typeof(Guid[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/roles/write", async (Guid eventId, IEventRolesService s, CancellationToken ct) => Results.Ok(await s.GetWriteIdsAsync(eventId, ct)), "SeaListEventWriteRoles", typeof(Guid[]));
        _ = group.MapModulePost(SeaApiRoutes.EventRoles, async (C.EventRoleWriteDto r, IEventRolesService s, CancellationToken ct) => { await s.AssignAsync(r.EventId, r.RoleId, r.Type == CD.RoleType.Write, ct); return Results.Ok(new SeaCommandResult(true)); }, "SeaAssignEventRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/event-roles/{id:guid}", async (Guid id, IEventRolesService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); }, "SeaRemoveEventRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet(SeaApiRoutes.Statistics, async (IDatabaseStatisticsService s, CancellationToken ct) => Results.Ok(ToContract<C.DatabaseStatisticsReadDto>(await s.GetAsync(ct))), "SeaGetStatistics", typeof(C.DatabaseStatisticsReadDto));
        return endpoints;
    }

    private static IResult OkList<T>(IEnumerable<object> values) => Results.Ok(values.Select(ToContract<T>).ToArray());
    private static IResult Maybe<TIn,TOut>(TIn? value, Func<TIn,TOut> map) where TIn : class => value is null ? Results.NotFound(new SeaError(SeaErrorCode.NotFound, "Sea resource was not found.")) : Results.Ok(map(value));
    private static IResult Command(object? entity) => entity is null ? Results.NotFound(new SeaError(SeaErrorCode.NotFound, "Sea resource was not found.")) : Results.Ok(new SeaCommandResult(true, (Guid?)entity.GetType().GetProperty("Id")?.GetValue(entity)));
    private static async Task<IResult> MarkAsync(Func<Guid,CancellationToken,Task> action, IRequestContext context, CancellationToken ct, Guid id){ await action(UserGuid(context), ct); return Results.Ok(new SeaCommandResult(true, id)); }
    private static Guid UserGuid(IRequestContext context) => Guid.TryParse(context.UserId, out Guid id) ? id : Guid.Empty;
    private static T ToContract<T>(object value) => SeaContractApplicationMapper.Convert<T>(value);
    private static T ToDomain<T>(object value) => SeaContractApplicationMapper.Convert<T>(value);
}
