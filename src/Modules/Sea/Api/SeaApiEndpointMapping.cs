using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Sea.Api.Authorization;
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

        _ = group.MapModuleGet(SeaApiRoutes.Health, () => Results.Ok(new SeaCommandResult(true)), "SeaGetHealth", typeof(SeaCommandResult));
        _ = group.MapModuleGet("/capabilities", GetCapabilitiesAsync, "SeaGetCapabilities", typeof(C.SeaCapabilitiesReadDto));

        MapProjectEndpoints(group);
        MapProcedureEndpoints(group);
        MapEventEndpoints(group);
        MapAttachmentEndpoints(group);
        MapRoleEndpoints(group);
        MapStatisticsEndpoint(group);
        return endpoints;
    }

    private static void MapProjectEndpoints(RouteGroupBuilder group)
    {
        _ = group.MapModuleGet(SeaApiRoutes.Projects, ListProjectsAsync, "SeaListProjects", typeof(C.ProjectReadDto[]));
        _ = group.MapModuleGet("/projects/{id:guid}", GetProjectAsync, "SeaGetProject", typeof(C.ProjectReadDto));
        _ = group.MapModulePost(SeaApiRoutes.Projects, CreateProjectAsync, "SeaCreateProject", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Projects, UpdateProjectAsync, "SeaUpdateProject", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/projects/{id:guid}", DeleteProjectAsync, "SeaDeleteProject", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet("/projects/{id:guid}/snapshot", GetProjectSnapshotAsync, "SeaGetProjectSnapshot", typeof(C.ProjectSnapshot));
        _ = group.MapModulePost("/projects/{id:guid}/read", MarkProjectReadAsync, "SeaMarkProjectRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/projects/{id:guid}/unread", MarkProjectUnreadAsync, "SeaMarkProjectUnread", typeof(SeaCommandResult));
    }

    private static void MapProcedureEndpoints(RouteGroupBuilder group)
    {
        _ = group.MapModuleGet("/projects/{projectId:guid}/procedures", ListProceduresAsync, "SeaListProcedures", typeof(C.ProcedureReadDto[]));
        _ = group.MapModuleGet("/procedures/{id:guid}", GetProcedureAsync, "SeaGetProcedure", typeof(C.ProcedureReadDto));
        _ = group.MapModulePost(SeaApiRoutes.Procedures, CreateProcedureAsync, "SeaCreateProcedure", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Procedures, UpdateProcedureAsync, "SeaUpdateProcedure", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/procedures/{id:guid}", DeleteProcedureAsync, "SeaDeleteProcedure", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModulePost("/procedures/{id:guid}/read", MarkProcedureReadAsync, "SeaMarkProcedureRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/procedures/{id:guid}/unread", MarkProcedureUnreadAsync, "SeaMarkProcedureUnread", typeof(SeaCommandResult));
    }

    private static void MapEventEndpoints(RouteGroupBuilder group)
    {
        _ = group.MapModuleGet("/procedures/{procedureId:guid}/events", ListEventsAsync, "SeaListEvents", typeof(C.EventReadDto[]));
        _ = group.MapModuleGet("/events/{id:guid}", GetEventAsync, "SeaGetEvent", typeof(C.EventReadDto));
        _ = group.MapModuleGet("/events/{id:guid}/capabilities", GetEventCapabilitiesAsync, "SeaGetEventCapabilities", typeof(C.EventCapabilitiesReadDto));
        _ = group.MapModuleGet(
            "/events/{id:guid}/permissions/write",
            GetEventWritePermissionAsync,
            "SeaGetEventWritePermission",
            typeof(bool),
            deprecated: true);
        _ = group.MapModulePost("/events/after/{previousEventId:guid}", CreateEventAfterAsync, "SeaCreateEventAfter", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/after", CreateFirstEventAsync, "SeaCreateFirstEvent", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Events, UpdateEventAsync, "SeaUpdateEvent", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/events/{id:guid}", DeleteEventAsync, "SeaDeleteEvent", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModulePost("/events/{id:guid}/move-up", MoveEventUpAsync, "SeaMoveEventUp", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/move-down", MoveEventDownAsync, "SeaMoveEventDown", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/commit", CommitEventAsync, "SeaCommitEvent", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/uncommit", UncommitEventAsync, "SeaUncommitEvent", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/read", MarkEventReadAsync, "SeaMarkEventRead", typeof(SeaCommandResult));
        _ = group.MapModulePost("/events/{id:guid}/unread", MarkEventUnreadAsync, "SeaMarkEventUnread", typeof(SeaCommandResult));
    }

    private static void MapAttachmentEndpoints(RouteGroupBuilder group)
    {
        _ = group.MapModuleGet("/events/{eventId:guid}/attachments", ListAttachmentsAsync, "SeaListAttachments", typeof(C.AttachmentReadDto[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/attachments/count", CountAttachmentsAsync, "SeaGetAttachmentCount", typeof(int));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/text", AddTextAttachmentAsync, "SeaCreateTextAttachment", typeof(SeaCommandResult));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/file", AddFileAttachmentAsync, "SeaCreateFileAttachment", typeof(SeaCommandResult));
        _ = group.MapModulePost(SeaApiRoutes.Attachments + "/batch", AddAttachmentBatchAsync, "SeaCreateAttachmentBatch", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Attachments + "/text", UpdateTextAttachmentAsync, "SeaUpdateTextAttachment", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/attachments/{id:guid}", DeleteAttachmentAsync, "SeaDeleteAttachment", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
    }

    private static void MapRoleEndpoints(RouteGroupBuilder group)
    {
        _ = group.MapModuleGet(SeaApiRoutes.Roles, ListRolesAsync, "SeaListRoles", typeof(C.RoleReadDto[]));
        _ = group.MapModulePost(SeaApiRoutes.Roles, CreateRoleAsync, "SeaCreateRole", typeof(SeaCommandResult));
        _ = group.MapModulePut(SeaApiRoutes.Roles, UpdateRoleAsync, "SeaUpdateRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/roles/{id:guid}", DeleteRoleAsync, "SeaDeleteRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet("/users/{userId}/roles", ListUserRolesAsync, "SeaListUserRoles", typeof(C.UserRoleReadDto[]));
        _ = group.MapModulePost(SeaApiRoutes.UserRoles, AssignUserRoleAsync, "SeaAssignUserRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/user-roles/{id:guid}", RemoveUserRoleAsync, "SeaRemoveUserRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
        _ = group.MapModuleGet("/events/{eventId:guid}/roles", ListEventRolesAsync, "SeaListEventRoles", typeof(Guid[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/roles/read", ListEventReadRolesAsync, "SeaListEventReadRoles", typeof(Guid[]));
        _ = group.MapModuleGet("/events/{eventId:guid}/roles/write", ListEventWriteRolesAsync, "SeaListEventWriteRoles", typeof(Guid[]));
        _ = group.MapModulePost(SeaApiRoutes.EventRoles, AssignEventRoleAsync, "SeaAssignEventRole", typeof(SeaCommandResult));
        _ = group.MapModuleDelete("/event-roles/{id:guid}", RemoveEventRoleAsync, "SeaRemoveEventRole", typeof(SeaCommandResult), successStatusCode: StatusCodes.Status200OK);
    }

    private static void MapStatisticsEndpoint(RouteGroupBuilder group) =>
        _ = group.MapModuleGet(SeaApiRoutes.Statistics, GetStatisticsAsync, "SeaGetStatistics", typeof(C.DatabaseStatisticsReadDto));

    private static async Task<IResult> GetCapabilitiesAsync(ClaimsPrincipal user, IAuthorizationService authorization) =>
        Results.Ok(new C.SeaCapabilitiesReadDto((await authorization.AuthorizeAsync(user, SeaAuthorizationPolicies.Manage)).Succeeded));

    private static async Task<IResult> ListProjectsAsync(
        ClaimsPrincipal user,
        IProjectsService service,
        IAuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<D.ProjectReadDto> projects = (await service.GetListAsync(new D.ProjectDescriptorDto(), cancellationToken)).Value ?? [];
        List<C.ProjectReadDto> visible = [];
        foreach (D.ProjectReadDto project in projects)
        {
            if (await CanAsync(authorization, user, new SeaProjectResource(project.Id), SeaAuthorizationOperations.Read))
            {
                visible.Add(ToContract<C.ProjectReadDto>(project));
            }
        }
        return Results.Ok(visible);
    }

    private static async Task<IResult> GetProjectAsync(Guid id, ClaimsPrincipal user, IProjectsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        D.ProjectReadDto? project = (await service.GetAsync(id, cancellationToken)).Value;
        if (project is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProjectResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        return Results.Ok(ToContract<C.ProjectReadDto>(project));
    }

    private static async Task<IResult> CreateProjectAsync(C.ProjectWriteDto request, ClaimsPrincipal user, IProjectsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.AddAsync(ToDomain<D.ProjectWriteDto>(request), cancellationToken));
    }

    private static async Task<IResult> UpdateProjectAsync(C.ProjectWriteDto request, ClaimsPrincipal user, IProjectsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(request.Id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.UpdateAsync(ToDomain<D.ProjectWriteDto>(request), cancellationToken));
    }

    private static async Task<IResult> DeleteProjectAsync(Guid id, ClaimsPrincipal user, IProjectsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        EntityResult result = await service.RemoveAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(new SeaCommandResult(true, id)) : EntityFailure(result.Errors);
    }

    private static async Task<IResult> GetProjectSnapshotAsync(Guid id, ClaimsPrincipal user, IProjectsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        D.ProjectReadDto? project = (await service.GetAsync(id, cancellationToken)).Value;
        if (project is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProjectResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        D.ProjectSnapshot? snapshot = await service.CaptureSnapshotAsync(id, cancellationToken);
        return snapshot is null ? NotFound() : Results.Ok(ToContract<C.ProjectSnapshot>(snapshot));
    }

    private static Task<IResult> MarkProjectReadAsync(Guid id, ClaimsPrincipal user, IProjectsService projects, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkProjectAsync(id, true, user, projects, receipts, authorization, cancellationToken);

    private static Task<IResult> MarkProjectUnreadAsync(Guid id, ClaimsPrincipal user, IProjectsService projects, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkProjectAsync(id, false, user, projects, receipts, authorization, cancellationToken);

    private static async Task<IResult> MarkProjectAsync(Guid id, bool read, ClaimsPrincipal user, IProjectsService projects, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await projects.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProjectResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        Guid userId = UserGuid(user);
        if (read) await receipts.MarkProjectReadAsync(userId, id, cancellationToken);
        else await receipts.MarkProjectUnreadAsync(userId, id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static async Task<IResult> ListProceduresAsync(Guid projectId, ClaimsPrincipal user, IProjectsService projects, IProceduresService procedures, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await projects.GetAsync(projectId, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProjectResource(projectId), SeaAuthorizationOperations.Read)) return Results.Forbid();
        IReadOnlyList<D.ProcedureReadDto> values = (await procedures.GetListAsync(new D.ProcedureDescriptorDto { ProjectId = projectId }, cancellationToken)).Value ?? [];
        List<C.ProcedureReadDto> visible = [];
        foreach (D.ProcedureReadDto procedure in values)
        {
            if (await CanAsync(authorization, user, new SeaProcedureResource(procedure.Id), SeaAuthorizationOperations.Read))
            {
                visible.Add(ToContract<C.ProcedureReadDto>(procedure));
            }
        }
        return Results.Ok(visible);
    }

    private static async Task<IResult> GetProcedureAsync(Guid id, ClaimsPrincipal user, IProceduresService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        D.ProcedureReadDto? procedure = (await service.GetAsync(id, cancellationToken)).Value;
        if (procedure is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProcedureResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        return Results.Ok(ToContract<C.ProcedureReadDto>(procedure));
    }

    private static async Task<IResult> CreateProcedureAsync(C.ProcedureWriteDto request, ClaimsPrincipal user, IProceduresService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.AddAsync(ToDomain<D.ProcedureWriteDto>(request), cancellationToken));
    }

    private static async Task<IResult> UpdateProcedureAsync(C.ProcedureWriteDto request, ClaimsPrincipal user, IProceduresService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(request.Id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.UpdateAsync(ToDomain<D.ProcedureWriteDto>(request), cancellationToken));
    }

    private static async Task<IResult> DeleteProcedureAsync(Guid id, ClaimsPrincipal user, IProceduresService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.DeleteAsync(id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static Task<IResult> MarkProcedureReadAsync(Guid id, ClaimsPrincipal user, IProceduresService procedures, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkProcedureAsync(id, true, user, procedures, receipts, authorization, cancellationToken);

    private static Task<IResult> MarkProcedureUnreadAsync(Guid id, ClaimsPrincipal user, IProceduresService procedures, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkProcedureAsync(id, false, user, procedures, receipts, authorization, cancellationToken);

    private static async Task<IResult> MarkProcedureAsync(Guid id, bool read, ClaimsPrincipal user, IProceduresService procedures, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await procedures.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProcedureResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        Guid userId = UserGuid(user);
        if (read) await receipts.MarkProcedureReadAsync(userId, id, cancellationToken);
        else await receipts.MarkProcedureUnreadAsync(userId, id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static async Task<IResult> ListEventsAsync(Guid procedureId, ClaimsPrincipal user, IProceduresService procedures, IEventsService events, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await procedures.GetAsync(procedureId, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaProcedureResource(procedureId), SeaAuthorizationOperations.Read)) return Results.Forbid();
        IReadOnlyList<D.EventReadDto> values = await events.GetByProcedureAsync(procedureId, cancellationToken);
        List<C.EventReadDto> result = new(values.Count);
        foreach (D.EventReadDto @event in values)
        {
            C.EventReadDto mapped = ToContract<C.EventReadDto>(@event);
            if (!await CanAsync(authorization, user, new SeaEventResource(@event.Id), SeaAuthorizationOperations.Read))
            {
                mapped.Title = "Redacted";
                mapped.Description = "Redacted";
                mapped.Stats = null;
            }
            result.Add(mapped);
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        D.EventReadDto? @event = (await service.GetAsync(id, cancellationToken)).Value;
        if (@event is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        return Results.Ok(ToContract<C.EventReadDto>(@event));
    }

    private static async Task<IResult> GetEventCapabilitiesAsync(Guid id, ClaimsPrincipal user, IEventsService events, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        bool canRead = await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Read);
        bool canWrite = await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Write);
        bool canManage = await CanManageAsync(authorization, user);
        return Results.Ok(new C.EventCapabilitiesReadDto(canRead, canWrite, canManage));
    }

    private static async Task<IResult> GetEventWritePermissionAsync(Guid id, ClaimsPrincipal user, IEventsService events, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        return Results.Ok(await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Write));
    }

    private static Task<IResult> CreateEventAfterAsync(Guid previousEventId, C.EventWriteDto request, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        CreateEventAsync(previousEventId, request, user, service, authorization, cancellationToken);

    private static Task<IResult> CreateFirstEventAsync(C.EventWriteDto request, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        CreateEventAsync(null, request, user, service, authorization, cancellationToken);

    private static async Task<IResult> CreateEventAsync(Guid? previousEventId, C.EventWriteDto request, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.AddAsync(request.ProjectId, request.ProcedureId, previousEventId, cancellationToken));
    }

    private static async Task<IResult> UpdateEventAsync(C.EventWriteDto request, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(request.Id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Command(await service.UpdateAsync(request.Id, request.Title ?? string.Empty, request.Description ?? string.Empty, cancellationToken));
    }

    private static async Task<IResult> DeleteEventAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Results.Ok(new SeaCommandResult(await service.RemoveEventAsync(Guid.Empty, id, cancellationToken), id));
    }

    private static Task<IResult> MoveEventUpAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MoveEventAsync(id, true, user, service, authorization, cancellationToken);

    private static Task<IResult> MoveEventDownAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MoveEventAsync(id, false, user, service, authorization, cancellationToken);

    private static async Task<IResult> MoveEventAsync(Guid id, bool up, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        bool moved = up
            ? await service.MoveUpAsync(Guid.Empty, id, cancellationToken)
            : await service.MoveDownAsync(Guid.Empty, id, cancellationToken);
        return Results.Ok(new SeaCommandResult(moved, id));
    }

    private static Task<IResult> CommitEventAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        SetEventCommitAsync(id, true, user, service, authorization, cancellationToken);

    private static Task<IResult> UncommitEventAsync(Guid id, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        SetEventCommitAsync(id, false, user, service, authorization, cancellationToken);

    private static async Task<IResult> SetEventCommitAsync(Guid id, bool commit, ClaimsPrincipal user, IEventsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Write)) return Results.Forbid();
        if (commit) await service.CommitAsync(id, UserGuid(user), DateTime.UtcNow, cancellationToken);
        else await service.UncommitAsync(id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static Task<IResult> MarkEventReadAsync(Guid id, ClaimsPrincipal user, IEventsService events, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkEventAsync(id, true, user, events, receipts, authorization, cancellationToken);

    private static Task<IResult> MarkEventUnreadAsync(Guid id, ClaimsPrincipal user, IEventsService events, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        MarkEventAsync(id, false, user, events, receipts, authorization, cancellationToken);

    private static async Task<IResult> MarkEventAsync(Guid id, bool read, ClaimsPrincipal user, IEventsService events, IReceiptsService receipts, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(id, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(id), SeaAuthorizationOperations.Read)) return Results.Forbid();
        Guid userId = UserGuid(user);
        if (read) await receipts.MarkEventReadAsync(userId, id, cancellationToken);
        else await receipts.MarkEventUnreadAsync(userId, id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static async Task<IResult> ListAttachmentsAsync(Guid eventId, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(eventId), SeaAuthorizationOperations.Read)) return Results.Forbid();
        return OkList<C.AttachmentReadDto>(await attachments.GetByEventAsync(eventId, cancellationToken));
    }

    private static async Task<IResult> CountAttachmentsAsync(Guid eventId, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(eventId), SeaAuthorizationOperations.Read)) return Results.Forbid();
        return Results.Ok(await attachments.GetCountByEventAsync(eventId, cancellationToken));
    }

    private static Task<IResult> AddTextAttachmentAsync(C.AttachmentWriteDto request, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        AddAttachmentAsync(request, DD.AttachmentKind.Text, user, events, attachments, authorization, cancellationToken);

    private static Task<IResult> AddFileAttachmentAsync(C.AttachmentWriteDto request, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken) =>
        AddAttachmentAsync(request, DD.AttachmentKind.File, user, events, attachments, authorization, cancellationToken);

    private static async Task<IResult> AddAttachmentAsync(C.AttachmentWriteDto request, DD.AttachmentKind kind, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(request.EventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaEventResource(request.EventId), SeaAuthorizationOperations.Write)) return Results.Forbid();
        return Command(await attachments.AddAsync(request.EventId, request.Content, request.ContentType, request.ContentThumbnail, request.Size, request.FileName, kind, cancellationToken));
    }

    private static async Task<IResult> AddAttachmentBatchAsync(IReadOnlyList<C.AttachmentWriteDto> requests, ClaimsPrincipal user, IEventsService events, IAttachmentsService attachments, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        foreach (Guid eventId in requests.Select(request => request.EventId).Distinct())
        {
            if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
            if (!await CanAsync(authorization, user, new SeaEventResource(eventId), SeaAuthorizationOperations.Write)) return Results.Forbid();
        }
        D.AttachmentWriteDto[] items = [.. requests.Select(request => new D.AttachmentWriteDto(
            request.EventId,
            request.Content,
            request.ContentType,
            request.ContentThumbnail,
            request.Size,
            (DD.AttachmentKind)(int)request.Kind,
            request.FileName)
        {
            Id = request.Id
        })];
        return Command(await attachments.AddBatchAsync(items, cancellationToken));
    }

    private static async Task<IResult> UpdateTextAttachmentAsync(C.AttachmentWriteDto request, ClaimsPrincipal user, IAttachmentsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await service.GetAsync(request.Id, cancellationToken)).Value is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaAttachmentResource(request.Id), SeaAuthorizationOperations.Write)) return Results.Forbid();
        return Command(await service.UpdateAsync(request.EventId, request.Id, request.Content, cancellationToken));
    }

    private static async Task<IResult> DeleteAttachmentAsync(Guid id, ClaimsPrincipal user, IAttachmentsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        D.AttachmentReadDto? attachment = (await service.GetAsync(id, cancellationToken)).Value;
        if (attachment is null) return NotFound();
        if (!await CanAsync(authorization, user, new SeaAttachmentResource(id), SeaAuthorizationOperations.Write)) return Results.Forbid();
        await service.RemoveAsync(attachment.EventId, id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static async Task<IResult> ListRolesAsync(ClaimsPrincipal user, IRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return OkList<C.RoleReadDto>((await service.GetListAsync(new D.RoleDescriptorDto(), cancellationToken)).Value ?? []);
    }

    private static async Task<IResult> CreateRoleAsync(C.RoleWriteDto request, ClaimsPrincipal user, IRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Results.Ok(new SeaCommandResult(true, await service.AddAsync(request.Name, cancellationToken)));
    }

    private static async Task<IResult> UpdateRoleAsync(C.RoleWriteDto request, ClaimsPrincipal user, IRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.UpdateAsync(request.Id, request.Name, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, request.Id));
    }

    private static async Task<IResult> DeleteRoleAsync(Guid id, ClaimsPrincipal user, IRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.RemoveAsync(id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true, id));
    }

    private static async Task<IResult> ListUserRolesAsync(string userId, ClaimsPrincipal user, IUserRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out Guid id)) return Results.BadRequest(new SeaError(SeaErrorCode.ValidationFailure, "User ID must be a GUID."));
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return OkList<C.UserRoleReadDto>((await service.GetListAsync(new D.UserRoleDescriptorDto { UserId = id }, cancellationToken)).Value ?? []);
    }

    private static async Task<IResult> AssignUserRoleAsync(C.UserRoleWriteDto request, ClaimsPrincipal user, IUserRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.AssignAsync(request.Id, request.RoleId, (DD.RoleType)(int)request.Type, cancellationToken);
        return Results.Ok(new SeaCommandResult(true));
    }

    private static async Task<IResult> RemoveUserRoleAsync(Guid id, ClaimsPrincipal user, IUserRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.RemoveAsync(UserGuid(user), id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true));
    }

    private static async Task<IResult> ListEventRolesAsync(Guid eventId, ClaimsPrincipal user, IEventsService events, IEventRolesService roles, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        var ids = await roles.GetIdsAsync(eventId, cancellationToken);
        return Results.Ok(ids.ReadRoleIds.Concat(ids.WriteRoleIds).Distinct().ToArray());
    }

    private static async Task<IResult> ListEventReadRolesAsync(Guid eventId, ClaimsPrincipal user, IEventsService events, IEventRolesService roles, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Results.Ok(await roles.GetReadIdsAsync(eventId, cancellationToken));
    }

    private static async Task<IResult> ListEventWriteRolesAsync(Guid eventId, ClaimsPrincipal user, IEventsService events, IEventRolesService roles, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if ((await events.GetAsync(eventId, cancellationToken)).Value is null) return NotFound();
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Results.Ok(await roles.GetWriteIdsAsync(eventId, cancellationToken));
    }

    private static async Task<IResult> AssignEventRoleAsync(C.EventRoleWriteDto request, ClaimsPrincipal user, IEventRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.AssignAsync(request.EventId, request.RoleId, request.Type == CD.RoleType.Write, cancellationToken);
        return Results.Ok(new SeaCommandResult(true));
    }

    private static async Task<IResult> RemoveEventRoleAsync(Guid id, ClaimsPrincipal user, IEventRolesService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        await service.RemoveAsync(id, cancellationToken);
        return Results.Ok(new SeaCommandResult(true));
    }

    private static async Task<IResult> GetStatisticsAsync(ClaimsPrincipal user, IDatabaseStatisticsService service, IAuthorizationService authorization, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(authorization, user)) return Results.Forbid();
        return Results.Ok(ToContract<C.DatabaseStatisticsReadDto>(await service.GetAsync(cancellationToken)));
    }

    private static async Task<bool> CanManageAsync(IAuthorizationService authorization, ClaimsPrincipal user) =>
        (await authorization.AuthorizeAsync(user, SeaAuthorizationPolicies.Manage)).Succeeded;

    private static async Task<bool> CanAsync(IAuthorizationService authorization, ClaimsPrincipal user, object resource, IAuthorizationRequirement requirement) =>
        (await authorization.AuthorizeAsync(user, resource, requirement)).Succeeded;

    private static IResult OkList<T>(IEnumerable<object> values) => Results.Ok(values.Select(ToContract<T>).ToArray());
    private static IResult NotFound() => Results.NotFound(new SeaError(SeaErrorCode.NotFound, "Sea resource was not found."));
    private static IResult Command<T>(EntityResult<T> result) where T : class => result.IsSuccess && result.Value is not null
        ? Results.Ok(new SeaCommandResult(true, (Guid?)result.Value.GetType().GetProperty("Id")?.GetValue(result.Value)))
        : EntityFailure(result.Errors);
    private static IResult Command(object? entity) => entity is null ? NotFound() : Results.Ok(new SeaCommandResult(true, (Guid?)entity.GetType().GetProperty("Id")?.GetValue(entity)));
    private static IResult EntityFailure(IReadOnlyList<EntityError> errors)
    {
        EntityError? error = errors.FirstOrDefault();
        return error?.Code switch
        {
            EntityErrorCode.Validation => Results.BadRequest(new SeaError(SeaErrorCode.ValidationFailure, error.Message)),
            EntityErrorCode.NotFound => NotFound(),
            EntityErrorCode.Conflict or EntityErrorCode.Concurrency => Results.Conflict(new SeaError(SeaErrorCode.Conflict, error.Message)),
            _ => Results.Problem(error?.Message ?? "SEA operation failed.")
        };
    }

    private static Guid UserGuid(ClaimsPrincipal user) =>
        AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success success && Guid.TryParse(success.UserId.Value, out Guid id)
            ? id
            : throw new InvalidOperationException("The authenticated SEA subject is not a GUID.");

    private static T ToContract<T>(object value) => SeaContractApplicationMapper.Convert<T>(value);
    private static T ToDomain<T>(object value) => SeaContractApplicationMapper.Convert<T>(value);
}
