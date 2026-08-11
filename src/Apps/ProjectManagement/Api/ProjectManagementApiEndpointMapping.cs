using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.ProjectManagement.Application;
using Vitarish.ProjectManagement.Contracts;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using MediatR;
using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.ProjectManagement.Api;

public static class ProjectManagementApiEndpointMapping
{
    public static IEndpointRouteBuilder MapProjectManagementApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ProjectManagementApiRoutes.Base);
        _ = group.MapModuleGet(ProjectManagementApiRoutes.Actions, GetAsync, "ProjectManagementGetEventAction", typeof(ProjectManagementActionResponse));
        _ = group.MapModulePost(ProjectManagementApiRoutes.ConfirmAction, ConfirmAsync, "ProjectManagementConfirmEventAction", typeof(ProjectManagementActionResponse));
        _ = group.MapModulePost(ProjectManagementApiRoutes.RejectAction, RejectAsync, "ProjectManagementRejectEventAction", typeof(ProjectManagementActionResponse));
        _ = group.MapModuleDelete(ProjectManagementApiRoutes.Conversation, DeleteConversationAsync, "ProjectManagementDeleteEventConversation");
        _ = group.MapModuleGet(ProjectManagementApiRoutes.Conversation, GetEventConversationAsync, "ProjectManagementGetEventConversation", typeof(ProjectManagementThreadDisplay));
        return endpoints;
    }
    private static ProjectManagementThreadDisplay ToDisplay(AgentConversationDisplay value)
    {
        if (string.IsNullOrWhiteSpace(value.PresentationTranscript))
        {
            return new(value.Id.ToString("D"), value.Title, value.LatestTextPreview, value.CreatedAt, value.UpdatedAt, value.Transcript);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value.PresentationTranscript);
            List<ProjectManagementThreadTurn> turns = [];
            foreach (JsonElement turn in document.RootElement.EnumerateArray())
            {
                string user = turn.TryGetProperty("user", out JsonElement userValue) ? userValue.GetString() ?? string.Empty : string.Empty;
                string assistant = turn.TryGetProperty("assistant", out JsonElement assistantValue) ? assistantValue.GetString() ?? string.Empty : string.Empty;
                Guid[] actionIds = turn.TryGetProperty("preparedActionIds", out JsonElement actions) && actions.ValueKind == JsonValueKind.Array
                    ? actions.EnumerateArray().Where(x => x.TryGetGuid(out _)).Select(x => x.GetGuid()).ToArray() : [];
                IReadOnlyList<ProjectManagementPresentationPart> parts = turn.TryGetProperty("parts", out JsonElement rawParts)
                    ? JsonSerializer.Deserialize<IReadOnlyList<ProjectManagementPresentationPart>>(rawParts.GetRawText()) ?? [] : [];
                turns.Add(new(user, assistant, actionIds, parts));
            }
            return new(value.Id.ToString("D"), value.Title, value.LatestTextPreview, value.CreatedAt, value.UpdatedAt, value.Transcript, turns);
        }
        catch (JsonException)
        {
            return new(value.Id.ToString("D"), value.Title, value.LatestTextPreview, value.CreatedAt, value.UpdatedAt, value.Transcript);
        }
    }

    private static Task<IResult> GetAsync(Guid eventId, Guid actionId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, ProjectManagementActionWorkflows workflows, CancellationToken ct) =>
        ExecuteAsync(eventId, actionId, requestContext, users, seaAccess, workflows.GetAsync, ct);
    private static Task<IResult> ConfirmAsync(Guid eventId, Guid actionId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, ProjectManagementActionWorkflows workflows, CancellationToken ct) =>
        ExecuteAsync(eventId, actionId, requestContext, users, seaAccess, workflows.ConfirmAsync, ct);
    private static async Task<IResult> RejectAsync(Guid eventId, Guid actionId, RejectProjectManagementActionRequest request, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, ProjectManagementActionWorkflows workflows, CancellationToken ct)
    {
        ProjectManagementAgentContext? context = await ContextAsync(eventId, requestContext, users, seaAccess, ct);
        if (context is null)
        {
            return Results.Forbid();
        }

        ProjectManagementActionResult result = await workflows.RejectAsync(actionId, context, request?.Reason, ct);
        return ToResult(result);
    }
    private static async Task<IResult> ExecuteAsync(Guid eventId, Guid actionId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, Func<Guid, ProjectManagementAgentContext, CancellationToken, Task<ProjectManagementActionResult>> operation, CancellationToken ct)
    {
        ProjectManagementAgentContext? context = await ContextAsync(eventId, requestContext, users, seaAccess, ct);
        if (context is null)
        {
            return Results.Forbid();
        }

        return ToResult(await operation(actionId, context, ct));
    }
    private static async Task<ProjectManagementAgentContext?> ContextAsync(Guid eventId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, CancellationToken ct)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) || tenantId == Guid.Empty || string.IsNullOrWhiteSpace(requestContext.UserId))
        {
            return null;
        }

        bool member = await users.CheckUserRoleAsync(requestContext.UserId, IdentityDefinitions.PlatformAdmin, ct) ||
            await users.CheckUserRoleAsync(requestContext.UserId, $"{tenantId:D}:{IdentityDefinitions.TenantOwner}", ct) ||
            await users.CheckUserRoleAsync(requestContext.UserId, $"{tenantId:D}:{IdentityDefinitions.TenantUser}", ct);
        return member && await seaAccess.CanReadEventAsync(new(tenantId.ToString("D"), requestContext.UserId), eventId, ct) ? new(tenantId.ToString("D"), requestContext.UserId, eventId, "fa") : null;
    }
    private static async Task<IResult> DeleteConversationAsync(Guid eventId, Guid conversationId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, ISender sender, CancellationToken ct)
    {
        ProjectManagementAgentContext? context = await ContextAsync(eventId, requestContext, users, seaAccess, ct);
        if (context is null)
        {
            return Results.Forbid();
        }

        AgentConversationOutcome outcome = await sender.Send(new DeleteAgentConversation(conversationId, new(context.TenantId, context.UserId, "project-management.event", "event", eventId.ToString("D"))), ct);
        return outcome switch { AgentConversationOutcome.Succeeded => Results.NoContent(), AgentConversationOutcome.NotFound => Results.NotFound(), AgentConversationOutcome.Busy => Results.Conflict(), _ => Results.Problem() };
    }
    private static async Task<IResult> GetEventConversationAsync(Guid eventId, Guid conversationId, IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, IAgentConversationStore conversations, CancellationToken ct)
    {
        ProjectManagementAgentContext? context = await ContextAsync(eventId, requestContext, users, seaAccess, ct);
        if (context is null)
        {
            return Results.Forbid();
        }

        AgentConversationDisplay? display = await conversations.GetDisplayAsync(conversationId, new(context.TenantId, context.UserId, "project-management.event", "event", eventId.ToString("D")), ct);
        return display is null ? Results.NotFound() : Results.Ok(ToDisplay(display));
    }
    private static IResult ToResult(ProjectManagementActionResult result) => result.Outcome switch
    {
        ProjectManagementActionOutcome.Succeeded when result.Action is { } action => Results.Ok(new ProjectManagementActionResponse(action.ActionId.ToString("D"), action.Status.ToString(), Review(action), action.CompletionPayload, action.Failure?.Code, action.RejectionReason)),
        ProjectManagementActionOutcome.NotFound => Results.NotFound(),
        ProjectManagementActionOutcome.Forbidden => Results.Forbid(),
        ProjectManagementActionOutcome.Expired => Results.Conflict(),
        ProjectManagementActionOutcome.Conflict => Results.Conflict(),
        ProjectManagementActionOutcome.ValidationFailure => Results.BadRequest(),
        _ => Results.Problem(statusCode: StatusCodes.Status502BadGateway)
    };
    private static ProjectManagementActionReview Review(ProjectManagementAction action)
    {
        using JsonDocument payload = JsonDocument.Parse(action.PreparedAction.PayloadJson);
        JsonElement root = payload.RootElement;
        string? attachmentId = root.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        string operation = action.PreparedAction.Operation;
        string[] fields = operation switch
        {
            "create_project" or "update_project" => ["name", "description"],
            "create_procedure" or "update_procedure" => ["name", "description", "projectId"],
            "create_event" => ["title", "description", "projectId", "procedureId"],
            "update_event" => ["title", "description"],
            "create_reminder" or "update_reminder" => ["kind", "recipientUserId", "remindAt", "subject", "textBody"],
            "create_attachment" or "update_attachment" => ["eventId", "fileName", "contentType", "size", "kind"],
            _ => ["id"]
        };
        IReadOnlyList<ProjectManagementActionReviewChange> changes = fields
            .Where(field => root.TryGetProperty(field, out _))
            .Select(field => new ProjectManagementActionReviewChange(field, ReviewValue(root.GetProperty(field))))
            .ToArray();
        bool destructive = operation.StartsWith("delete_", StringComparison.Ordinal) || operation.StartsWith("uncommit_", StringComparison.Ordinal) || operation.StartsWith("remove_", StringComparison.Ordinal);
        return new(operation, action.EventId?.ToString("D") ?? action.SubjectId ?? string.Empty, attachmentId, string.Empty, changes, destructive, destructive ? "This action may permanently remove or undo SEA data." : null);
    }

    private static string? ReviewValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
        _ => null
    };
}
