using System.Text.Json;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;

public sealed class SeaManagementAgentCapability(SeaManagementTool tool) : PatientAgentCapability([tool], ["sea_manage"])
{
    public override string Name => "SeaManagement";
    public override string Description => "Operate the authenticated practice's SEA projects, procedures, events, attachments, receipts, roles, and statistics.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(PatientAgentContext context) =>
        context.SubjectKind == "practice" && PatientAgentOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() =>
        "Use sea_manage for authenticated-practice SEA operations. Mutations require confirmation. " +
        "Use exact IDs returned by SEA and report validation, authorization, not-found, and dependency failures without inventing success.";
}

public sealed class SeaManagementTool(
    IProjectsService projects,
    IProceduresService procedures,
    IEventsService events,
    IAttachmentsService attachments,
    IReceiptsService receipts,
    IRolesService roles,
    IUserRolesService userRoles,
    IEventRolesService eventRoles,
    IDatabaseStatisticsService statistics) : IPatientAgentPreparationTool
{
    private const int MaxContentLength = 1_048_576;
    public AgentToolDefinition Definition { get; } = new(
        "sea_manage",
        "Execute a supported SEA operation in the authenticated practice. Writes require confirmation.",
        """{"type":"object","properties":{"operation":{"type":"string","enum":["health","get_capabilities","list_projects","get_project","create_project","update_project","delete_project","get_project_snapshot","list_procedures","get_procedure","create_procedure","update_procedure","delete_procedure","list_events","get_event","get_event_capabilities","get_event_write_permission","create_event","update_event","delete_event","move_event_up","move_event_down","repair_event_chain","commit_event","commit_procedure_events","uncommit_event","uncommit_procedure_events","get_event_reminders_count","list_attachments","count_attachments","create_attachment","create_attachment_batch","update_attachment","delete_attachment","mark_read","mark_unread","list_roles","create_role","update_role","delete_role","list_user_roles","assign_user_role","remove_user_role","list_event_roles","get_event_read_roles","get_event_write_roles","assign_event_role","remove_event_role","get_statistics"]},"payload":{"type":"object"}},"required":["operation"],"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement root = document.RootElement;
        string operation = RequiredString(root, "operation");
        JsonElement payload = root.TryGetProperty("payload", out JsonElement supplied) ? supplied : default;
        bool commit = root.TryGetProperty("commit", out JsonElement commitValue) && commitValue.ValueKind == JsonValueKind.True;
        if (IsMutation(operation) && !commit)
        {
            Guid actionId = Guid.CreateVersion7();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            return new(call.Id, JsonSerializer.Serialize(new { status = "prepared", actionId, arguments = root.GetRawText(), expiresAt }));
        }
        object result = operation switch
        {
            "health" => new { Succeeded = true },
            "get_capabilities" => new { CanManage = true },
            "list_projects" => await ListProjects(payload, cancellationToken),
            "get_project" => await Get(projects, Id(payload), cancellationToken),
            "create_project" => await Add(projects, Read<ProjectWriteDto>(payload), cancellationToken),
            "update_project" => await Update(projects, Read<ProjectWriteDto>(payload), cancellationToken),
            "delete_project" => await Remove(projects, Id(payload), cancellationToken),
            "get_project_snapshot" => await projects.CaptureSnapshotAsync(Id(payload), cancellationToken) ?? throw new InvalidOperationException("SEA project was not found."),
            "list_procedures" => await ListProcedures(payload, cancellationToken),
            "get_procedure" => await Get(procedures, Id(payload), cancellationToken),
            "create_procedure" => await Add(procedures, Read<ProcedureWriteDto>(payload), cancellationToken),
            "update_procedure" => await Update(procedures, Read<ProcedureWriteDto>(payload), cancellationToken),
            "delete_procedure" => await DeleteProcedure(payload, cancellationToken),
            "list_events" => await ListEvents(payload, cancellationToken),
            "get_event" => await Get(events, Id(payload), cancellationToken),
            "get_event_capabilities" => await GetEventCapabilities(payload, cancellationToken),
            "get_event_write_permission" => await GetEventWritePermission(payload, cancellationToken),
            "create_event" => await CreateEvent(payload, cancellationToken),
            "update_event" => await UpdateEvent(payload, cancellationToken),
            "delete_event" => await DeleteEvent(payload, cancellationToken),
            "move_event_up" => await MoveEvent(payload, true, cancellationToken),
            "move_event_down" => await MoveEvent(payload, false, cancellationToken),
            "repair_event_chain" => await RepairEventChain(payload, cancellationToken),
            "commit_event" => await CommitEvent(payload, context, cancellationToken),
            "commit_procedure_events" => await CommitProcedureEvents(payload, context, cancellationToken),
            "uncommit_event" => await UncommitEvent(payload, cancellationToken),
            "uncommit_procedure_events" => await events.UncommitAllByProcedureAsync(Id(payload), cancellationToken),
            "get_event_reminders_count" => await events.GetRemindersCountAsync(Id(payload), cancellationToken),
            "list_attachments" => await ListAttachments(payload, cancellationToken),
            "count_attachments" => await attachments.GetCountByEventAsync(Id(payload), cancellationToken),
            "create_attachment" => await CreateAttachment(payload, cancellationToken),
            "create_attachment_batch" => await CreateAttachmentBatch(payload, cancellationToken),
            "update_attachment" => await UpdateAttachment(payload, cancellationToken),
            "delete_attachment" => await DeleteAttachment(payload, cancellationToken),
            "mark_read" or "mark_unread" => await MarkRead(payload, operation == "mark_read", cancellationToken),
            "list_roles" => await roles.GetPagedAsync(new RoleDescriptorDto(), cancellationToken),
            "create_role" => await roles.AddAsync(RequiredString(payload, "name"), cancellationToken),
            "update_role" => await UpdateRole(payload, cancellationToken),
            "delete_role" => await roles.RemoveAsync(Id(payload), cancellationToken),
            "list_user_roles" => await userRoles.GetListAsync(new UserRoleDescriptorDto { UserId = Id(payload) }, cancellationToken),
            "assign_user_role" => await AssignUserRole(payload, cancellationToken),
            "remove_user_role" => await RemoveUserRole(payload, cancellationToken),
            "list_event_roles" => await eventRoles.GetIdsAsync(Id(payload), cancellationToken),
            "get_event_read_roles" => await eventRoles.GetReadIdsAsync(Id(payload), cancellationToken),
            "get_event_write_roles" => await eventRoles.GetWriteIdsAsync(Id(payload), cancellationToken),
            "assign_event_role" => await AssignEventRole(payload, true, cancellationToken),
            "remove_event_role" => await AssignEventRole(payload, false, cancellationToken),
            "get_statistics" => await statistics.GetAsync(cancellationToken),
            _ => throw new JsonException($"Unsupported SEA operation: {operation}.")
        };
        return new(call.Id, JsonSerializer.Serialize(result));
    }

    public PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        using JsonDocument document = JsonDocument.Parse(result.Output);
        JsonElement root = document.RootElement;
        return root.TryGetProperty("status", out JsonElement status) && status.ValueKind == JsonValueKind.String && status.GetString() == "prepared" &&
            root.TryGetProperty("actionId", out JsonElement actionId) && actionId.TryGetGuid(out Guid id) &&
            root.TryGetProperty("arguments", out JsonElement arguments) &&
            root.TryGetProperty("expiresAt", out JsonElement expiresAt) && expiresAt.TryGetDateTimeOffset(out DateTimeOffset expiry)
            ? new("SeaManagement", "sea_manage", id, arguments.GetString() ?? throw new JsonException("Prepared SEA arguments are invalid."), expiry)
            : null;
    }

    private static bool IsMutation(string operation) => operation switch
    {
        "create_project" or "update_project" or "delete_project" or
        "create_procedure" or "update_procedure" or "delete_procedure" or
        "create_event" or "update_event" or "delete_event" or
        "move_event_up" or "move_event_down" or "repair_event_chain" or
        "commit_event" or "commit_procedure_events" or "uncommit_event" or "uncommit_procedure_events" or
        "create_attachment" or "create_attachment_batch" or "update_attachment" or "delete_attachment" or
        "mark_read" or "mark_unread" or "create_role" or "update_role" or "delete_role" or
        "assign_user_role" or "remove_user_role" or "assign_event_role" or "remove_event_role" => true,
        _ => false
    };

    private async Task<object> ListProjects(JsonElement payload, CancellationToken ct) =>
        (await projects.GetListAsync(Read<ProjectDescriptorDto>(payload), ct)).Value ?? [];
    private async Task<object> ListProcedures(JsonElement payload, CancellationToken ct) =>
        (await procedures.GetListAsync(Read<ProcedureDescriptorDto>(payload), ct)).Value ?? [];
    private async Task<object> ListEvents(JsonElement payload, CancellationToken ct) =>
        await events.GetByProcedureAsync(Id(payload), ct);
    private async Task<object> GetEventCapabilities(JsonElement p, CancellationToken ct)
    {
        _ = await Get(events, Id(p), ct);
        return new { CanRead = true, CanWrite = true, CanManage = true };
    }
    private async Task<object> GetEventWritePermission(JsonElement p, CancellationToken ct)
    {
        _ = await Get(events, Id(p), ct);
        return true;
    }
    private async Task<object> Get<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, Guid id, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto =>
        (await service.GetAsync(id, ct)).Value ?? throw new InvalidOperationException("SEA entity was not found.");
    private static async Task<object> Add<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TWrite value, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto =>
        (await service.AddAsync(value, ct)).Value ?? throw new InvalidOperationException("SEA entity could not be created.");
    private static async Task<object> Update<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, TWrite value, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto =>
        (await service.UpdateAsync(value, ct)).Value ?? throw new InvalidOperationException("SEA entity could not be updated.");
    private static async Task<object> Remove<TRead, TWrite, TDescriptor>(IEntityService<TRead, TWrite, TDescriptor> service, Guid id, CancellationToken ct)
        where TRead : EntityReadDto where TWrite : EntityWriteDto where TDescriptor : EntityDescriptorDto =>
        await service.RemoveAsync(id, ct);

    private async Task<object> CreateEvent(JsonElement p, CancellationToken ct) =>
        await events.AddAsync(GuidValue(p, "projectId"), GuidValue(p, "procedureId"), OptionalGuid(p, "previousEventId"), RequiredString(p, "title"), RequiredString(p, "description"), ct)
        ?? throw new InvalidOperationException("SEA event could not be created.");
    private async Task<object> UpdateEvent(JsonElement p, CancellationToken ct) =>
        await events.UpdateAsync(Id(p), RequiredString(p, "title"), RequiredString(p, "description"), ct)
        ?? throw new InvalidOperationException("SEA event could not be updated.");
    private async Task<object> DeleteEvent(JsonElement p, CancellationToken ct) => await events.RemoveEventAsync(GuidValue(p, "procedureId"), Id(p), ct);
    private async Task<object> DeleteProcedure(JsonElement p, CancellationToken ct) { await procedures.DeleteAsync(Id(p), ct); return true; }
    private async Task<object> MoveEvent(JsonElement p, bool up, CancellationToken ct) => up ? await events.MoveUpAsync(GuidValue(p, "procedureId"), Id(p), ct) : await events.MoveDownAsync(GuidValue(p, "procedureId"), Id(p), ct);
    private async Task<object> RepairEventChain(JsonElement p, CancellationToken ct) { await events.RepairChainAsync(Id(p), ct); return true; }
    private async Task<object> CommitEvent(JsonElement p, AgentExecutionContext context, CancellationToken ct) { await events.CommitAsync(Id(p), AuthenticatedUserId(context), DateTime.UtcNow, ct); return true; }
    private async Task<object> CommitProcedureEvents(JsonElement p, AgentExecutionContext context, CancellationToken ct) { await events.CommitAllByProcedureAsync(Id(p), AuthenticatedUserId(context), DateTime.UtcNow, ct); return true; }
    private async Task<object> UncommitEvent(JsonElement p, CancellationToken ct) { await events.UncommitAsync(Id(p), ct); return true; }
    private async Task<object> ListAttachments(JsonElement p, CancellationToken ct) => await attachments.GetByEventAsync(Id(p), ct);
    private async Task<object> CreateAttachment(JsonElement p, CancellationToken ct) =>
        (await attachments.AddAsync(GuidValue(p, "eventId"), Content(p), RequiredString(p, "contentType"), OptionalString(p, "contentThumbnail"), p.GetProperty("size").GetInt64(), OptionalString(p, "fileName"), Enum.Parse<AttachmentKind>(RequiredString(p, "kind"), true), ct)).Value
        ?? throw new InvalidOperationException("SEA attachment could not be created.");
    private async Task<object> CreateAttachmentBatch(JsonElement p, CancellationToken ct)
    {
        JsonElement items = p.GetProperty("items");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 100) throw new JsonException("items must contain between 1 and 100 attachments.");
        IReadOnlyList<AttachmentWriteDto> values = JsonSerializer.Deserialize<IReadOnlyList<AttachmentWriteDto>>(items.GetRawText()) ?? throw new JsonException("items is invalid.");
        return (await attachments.AddBatchAsync(values, ct)).Value ?? throw new InvalidOperationException("SEA attachments could not be created.");
    }
    private async Task<object> UpdateAttachment(JsonElement p, CancellationToken ct) => await attachments.UpdateAsync(GuidValue(p, "eventId"), Id(p), Content(p), ct) ?? throw new InvalidOperationException("SEA attachment could not be updated.");
    private async Task<object> DeleteAttachment(JsonElement p, CancellationToken ct) { await attachments.RemoveAsync(GuidValue(p, "eventId"), Id(p), ct); return true; }
    private async Task<object> MarkRead(JsonElement p, bool read, CancellationToken ct)
    {
        Guid userId = GuidValue(p, "userId");
        Guid objectId = Id(p);
        string kind = RequiredString(p, "kind");
        if (kind == "project") { if (read) await receipts.MarkProjectReadAsync(userId, objectId, ct); else await receipts.MarkProjectUnreadAsync(userId, objectId, ct); }
        else if (kind == "procedure") { if (read) await receipts.MarkProcedureReadAsync(userId, objectId, ct); else await receipts.MarkProcedureUnreadAsync(userId, objectId, ct); }
        else if (kind == "event") { if (read) await receipts.MarkEventReadAsync(userId, objectId, ct); else await receipts.MarkEventUnreadAsync(userId, objectId, ct); }
        else throw new JsonException("kind must be project, procedure, or event.");
        return true;
    }
    private async Task<object> UpdateRole(JsonElement p, CancellationToken ct) { await roles.UpdateAsync(Id(p), RequiredString(p, "name"), ct); return true; }
    private async Task<object> AssignUserRole(JsonElement p, CancellationToken ct) { await userRoles.AssignAsync(GuidValue(p, "userId"), GuidValue(p, "roleId"), Enum.Parse<RoleType>(RequiredString(p, "type"), true), ct); return true; }
    private async Task<object> RemoveUserRole(JsonElement p, CancellationToken ct) { await userRoles.RemoveAsync(GuidValue(p, "userId"), GuidValue(p, "roleId"), ct); return true; }
    private async Task<object> AssignEventRole(JsonElement p, bool assign, CancellationToken ct) { if (assign) await eventRoles.AssignAsync(Id(p), GuidValue(p, "roleId"), p.GetProperty("isWrite").GetBoolean(), ct); else await eventRoles.UnassignAsync(Id(p), GuidValue(p, "roleId"), p.GetProperty("isWrite").GetBoolean(), ct); return true; }

    private static T Read<T>(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Activator.CreateInstance<T>() : JsonSerializer.Deserialize<T>(value.GetRawText()) ?? throw new JsonException("payload is invalid.");
    private static Guid Id(JsonElement p) => GuidValue(p, "id");
    private static Guid AuthenticatedUserId(AgentExecutionContext context) =>
        context.Metadata is not null && context.Metadata.TryGetValue("authenticatedUserId", out string? value) && Guid.TryParse(value, out Guid id)
            ? id
            : throw new UnauthorizedAccessException("The authenticated user identity is required.");
    private static Guid GuidValue(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.TryGetGuid(out Guid id) ? id : throw new JsonException($"{name} must be a valid GUID.");
    private static Guid? OptionalGuid(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null && value.TryGetGuid(out Guid id) ? id : null;
    private static string RequiredString(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : throw new JsonException($"{name} is required.");
    private static string? OptionalString(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string Content(JsonElement p) { string value = RequiredString(p, "content"); return value.Length <= MaxContentLength ? value : throw new JsonException("content is too large."); }
}
