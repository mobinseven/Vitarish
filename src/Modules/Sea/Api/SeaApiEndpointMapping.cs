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

        _ = group.MapGet(SeaApiRoutes.Projects, async (IProjectsService s, CancellationToken ct) => OkList<C.ProjectReadDto>((await s.GetListAsync(new D.ProjectDescriptorDto(), ct)).Value ?? []));
        _ = group.MapGet("/projects/{id:guid}", async (Guid id, IProjectsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.ProjectReadDto>));
        _ = group.MapPost(SeaApiRoutes.Projects, async (C.ProjectWriteDto r, IProjectsService s, CancellationToken ct) => Command(await s.AddAsync(ToDomain<D.ProjectWriteDto>(r), ct)));
        _ = group.MapPut(SeaApiRoutes.Projects, async (C.ProjectWriteDto r, IProjectsService s, CancellationToken ct) => { await s.UpdateAsync(ToDomain<D.ProjectWriteDto>(r), ct); return Results.Ok(new SeaCommandResult(true, r.Id)); });
        _ = group.MapDelete("/projects/{id:guid}", async (Guid id, IProjectsService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapGet("/projects/{id:guid}/snapshot", async (Guid id, IProjectsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, p => ToContract<C.ProjectSnapshot>(p)));
        _ = group.MapPost("/projects/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProjectReadAsync(u,id,t), c, ct, id));
        _ = group.MapPost("/projects/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProjectUnreadAsync(u,id,t), c, ct, id));

        _ = group.MapGet("/projects/{projectId:guid}/procedures", async (Guid projectId, IProceduresService s, CancellationToken ct) => OkList<C.ProcedureReadDto>((await s.GetListAsync(new D.ProcedureDescriptorDto { ProjectId = projectId }, ct)).Value ?? []));
        _ = group.MapGet("/procedures/{id:guid}", async (Guid id, IProceduresService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.ProcedureReadDto>));
        _ = group.MapPost(SeaApiRoutes.Procedures, async (C.ProcedureWriteDto r, IProceduresService s, CancellationToken ct) => Command(await s.AddAsync(ToDomain<D.ProcedureWriteDto>(r), ct)));
        _ = group.MapPut(SeaApiRoutes.Procedures, async (C.ProcedureWriteDto r, IProceduresService s, CancellationToken ct) => { await s.UpdateAsync(ToDomain<D.ProcedureWriteDto>(r), ct); return Results.Ok(new SeaCommandResult(true, r.Id)); });
        _ = group.MapDelete("/procedures/{id:guid}", async (Guid id, IProceduresService s, CancellationToken ct) => { await s.DeleteAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapPost("/procedures/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProcedureReadAsync(u,id,t), c, ct, id));
        _ = group.MapPost("/procedures/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkProcedureUnreadAsync(u,id,t), c, ct, id));

        _ = group.MapGet("/procedures/{procedureId:guid}/events", async (Guid procedureId, IEventsService s, CancellationToken ct) => OkList<C.EventReadDto>(await s.GetByProcedureAsync(procedureId, ct)));
        _ = group.MapGet("/events/{id:guid}", async (Guid id, IEventsService s, CancellationToken ct) => Maybe((await s.GetAsync(id, ct)).Value, ToContract<C.EventReadDto>));
        _ = group.MapGet("/events/{id:guid}/permissions/write", async (Guid id, IAccessControlledEventsService s) => Results.Ok(await s.CanWriteEvent(id)));
        _ = group.MapPost("/events/after/{previousEventId:guid?}", async (Guid? previousEventId, C.EventWriteDto r, IEventsService s, CancellationToken ct) => Command(await s.AddAsync(r.ProjectId, r.ProcedureId, previousEventId, ct)));
        _ = group.MapPut(SeaApiRoutes.Events, async (C.EventWriteDto r, IEventsService s, CancellationToken ct) => Command(await s.UpdateAsync(r.Id, r.Title ?? string.Empty, r.Description ?? string.Empty, ct)));
        _ = group.MapDelete("/events/{id:guid}", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.RemoveEventAsync(Guid.Empty, id, ct), id)));
        _ = group.MapPost("/events/{id:guid}/move-up", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.MoveUpAsync(Guid.Empty, id, ct), id)));
        _ = group.MapPost("/events/{id:guid}/move-down", async (Guid id, IEventsService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(await s.MoveDownAsync(Guid.Empty, id, ct), id)));
        _ = group.MapPost("/events/{id:guid}/commit", async (Guid id, IEventsService s, IRequestContext c, CancellationToken ct) => { await s.CommitAsync(id, UserGuid(c), DateTime.UtcNow, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapPost("/events/{id:guid}/uncommit", async (Guid id, IEventsService s, CancellationToken ct) => { await s.UncommitAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapPost("/events/{id:guid}/read", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkEventReadAsync(u,id,t), c, ct, id));
        _ = group.MapPost("/events/{id:guid}/unread", async (Guid id, IReceiptsService s, IRequestContext c, CancellationToken ct) => await MarkAsync((u,t) => s.MarkEventUnreadAsync(u,id,t), c, ct, id));

        _ = group.MapGet("/events/{eventId:guid}/attachments", async (Guid eventId, IAttachmentsService s, CancellationToken ct) => OkList<C.AttachmentReadDto>(await s.GetByEventAsync(eventId, ct)));
        _ = group.MapGet("/events/{eventId:guid}/attachments/count", async (Guid eventId, IAttachmentsService s, CancellationToken ct) => Results.Ok(await s.GetCountByEventAsync(eventId, ct)));
        _ = group.MapPost(SeaApiRoutes.Attachments + "/text", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.AddAsync(r.EventId, r.Content, r.ContentType, r.ContentThumbnail, r.Size, ct)));
        _ = group.MapPost(SeaApiRoutes.Attachments + "/file", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.AddAsync(r.EventId, r.Content, r.ContentType, r.ContentThumbnail, r.Size, ct)));
        _ = group.MapPost(SeaApiRoutes.Attachments + "/batch", async (IReadOnlyList<C.AttachmentWriteDto> r, IAttachmentsService s, CancellationToken ct) => { foreach (var a in r) await s.AddAsync(a.EventId, a.Content, a.ContentType, a.ContentThumbnail, a.Size, ct); return Results.Ok(new SeaCommandResult(true)); });
        _ = group.MapPut(SeaApiRoutes.Attachments + "/text", async (C.AttachmentWriteDto r, IAttachmentsService s, CancellationToken ct) => Command(await s.UpdateAsync(r.EventId, r.Id, r.Content, ct)));
        _ = group.MapDelete("/attachments/{id:guid}", async (Guid id, IAttachmentsService s, CancellationToken ct) => { await s.RemoveAsync(Guid.Empty, id, ct); return Results.Ok(new SeaCommandResult(true, id)); });

        _ = group.MapGet(SeaApiRoutes.Roles, async (IRolesService s, CancellationToken ct) => OkList<C.RoleReadDto>((await s.GetListAsync(new D.RoleDescriptorDto(), ct)).Value ?? []));
        _ = group.MapPost(SeaApiRoutes.Roles, async (C.RoleWriteDto r, IRolesService s, CancellationToken ct) => Results.Ok(new SeaCommandResult(true, await s.AddAsync(r.Name, ct))));
        _ = group.MapPut(SeaApiRoutes.Roles, async (C.RoleWriteDto r, IRolesService s, CancellationToken ct) => { await s.UpdateAsync(r.Id, r.Name, ct); return Results.Ok(new SeaCommandResult(true, r.Id)); });
        _ = group.MapDelete("/roles/{id:guid}", async (Guid id, IRolesService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapGet("/users/{userId}/roles", async (string userId, IUserRolesService s, CancellationToken ct) => OkList<C.UserRoleReadDto>((await s.GetListAsync(new D.UserRoleDescriptorDto { UserId = Guid.Parse(userId) }, ct)).Value ?? []));
        _ = group.MapPost(SeaApiRoutes.UserRoles, async (C.UserRoleWriteDto r, IUserRolesService s, CancellationToken ct) => { await s.AssignAsync(r.Id, r.RoleId, (DD.RoleType)(int)r.Type, ct); return Results.Ok(new SeaCommandResult(true)); });
        _ = group.MapDelete("/user-roles/{id:guid}", async (Guid id, IUserRolesService s, IRequestContext c, CancellationToken ct) => { await s.RemoveAsync(UserGuid(c), id, ct); return Results.Ok(new SeaCommandResult(true)); });

        _ = group.MapGet("/events/{eventId:guid}/roles", async (Guid eventId, IEventRolesService s, CancellationToken ct) => { var ids = await s.GetIdsAsync(eventId, ct); return Results.Ok(ids.ReadRoleIds.Concat(ids.WriteRoleIds).Distinct().ToArray()); });
        _ = group.MapGet("/events/{eventId:guid}/roles/read", async (Guid eventId, IEventRolesService s, CancellationToken ct) => Results.Ok(await s.GetReadIdsAsync(eventId, ct)));
        _ = group.MapGet("/events/{eventId:guid}/roles/write", async (Guid eventId, IEventRolesService s, CancellationToken ct) => Results.Ok(await s.GetWriteIdsAsync(eventId, ct)));
        _ = group.MapPost(SeaApiRoutes.EventRoles, async (C.EventRoleWriteDto r, IEventRolesService s, CancellationToken ct) => { await s.AssignAsync(r.EventId, r.RoleId, r.Type == CD.RoleType.Write, ct); return Results.Ok(new SeaCommandResult(true)); });
        _ = group.MapDelete("/event-roles/{id:guid}", async (Guid id, IEventRolesService s, CancellationToken ct) => { await s.RemoveAsync(id, ct); return Results.Ok(new SeaCommandResult(true, id)); });
        _ = group.MapGet(SeaApiRoutes.Statistics, async (IDatabaseStatisticsService s, CancellationToken ct) => Results.Ok(ToContract<C.DatabaseStatisticsReadDto>(await s.GetAsync(ct))));
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
