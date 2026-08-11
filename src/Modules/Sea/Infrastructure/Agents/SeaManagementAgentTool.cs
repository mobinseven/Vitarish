using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Agents;

public sealed class SeaManagementAgentTool(
    IProjectsService projects,
    IProceduresService procedures,
    IEventsService events,
    IAttachmentsService attachments,
    IReceiptsService receipts,
    IRolesService roles,
    IUserRolesService userRoles,
    IEventRolesService eventRoles,
    IDatabaseStatisticsService statistics,
    ISeaManagementAccessEvaluator access,
    IRemindersService? reminders = null,
    IUsersService? users = null) : ISeaManagementAgentTool
{
    private const int MaxContentLength = 1_048_576;
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public AgentToolDefinition Definition { get; } = new(
        "sea_manage",
        "Execute a supported SEA operation in the authenticated practice. Writes require confirmation.",
        """{"type":"object","properties":{"operation":{"type":"string","enum":["health","get_capabilities","list_projects","get_project","create_project","update_project","delete_project","get_project_snapshot","list_procedures","get_procedure","create_procedure","update_procedure","delete_procedure","list_events","get_event","get_event_capabilities","get_event_write_permission","create_event","update_event","delete_event","move_event_up","move_event_down","repair_event_chain","commit_event","commit_procedure_events","uncommit_event","uncommit_procedure_events","get_event_reminders_count","list_reminders","get_reminder","create_reminder","update_reminder","delete_reminder","list_attachments","count_attachments","create_attachment","create_attachment_batch","update_attachment","delete_attachment","mark_read","mark_unread","list_roles","create_role","update_role","delete_role","list_user_roles","assign_user_role","remove_user_role","list_event_roles","get_event_read_roles","get_event_write_roles","assign_event_role","remove_event_role","get_statistics"]},"payload":{"type":"object"}},"required":["operation"],"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement root = document.RootElement;
        string operation = RequiredString(root, "operation");
        JsonElement payload = root.TryGetProperty("payload", out JsonElement supplied) ? supplied : default;
        string payloadJson = payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? "{}" : payload.GetRawText();
        SeaManagementAgentSubject subject = Subject(context);
        if (!await access.IsAuthorizedAsync(subject, operation, payloadJson, cancellationToken))
        {
            throw new UnauthorizedAccessException("The SEA operation is not permitted.");
        }

        if (IsMutation(operation))
        {
            Guid actionId = Guid.CreateVersion7();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            return new(call.Id, JsonSerializer.Serialize(new SeaManagementPreparedAction(actionId, operation, payloadJson, expiresAt)));
        }
        return await ExecuteOperationAsync(call.Id, operation, payload, context, subject, cancellationToken);
    }

    private async Task<AgentToolResult> ExecuteOperationAsync(string callId, string operation, JsonElement payload, AgentExecutionContext context, SeaManagementAgentSubject subject, CancellationToken cancellationToken)
    {
        object result = operation switch
        {
            "health" => new { Succeeded = true },
            "get_capabilities" => new { CanManage = await access.IsAuthorizedAsync(subject, "get_statistics", "{}", cancellationToken) },
            "list_projects" => await ListProjects(payload, subject, cancellationToken),
            "get_project" => await Get(projects, Id(payload), cancellationToken),
            "create_project" => await Add(projects, Read<ProjectWriteDto>(payload), cancellationToken),
            "update_project" => await Update(projects, Read<ProjectWriteDto>(payload), cancellationToken),
            "delete_project" => await Remove(projects, Id(payload), cancellationToken),
            "get_project_snapshot" => await projects.CaptureSnapshotAsync(Id(payload), cancellationToken) ?? throw new InvalidOperationException("SEA project was not found."),
            "list_procedures" => await ListProcedures(payload, subject, cancellationToken),
            "get_procedure" => await Get(procedures, Id(payload), cancellationToken),
            "create_procedure" => await Add(procedures, Read<ProcedureWriteDto>(payload), cancellationToken),
            "update_procedure" => await Update(procedures, Read<ProcedureWriteDto>(payload), cancellationToken),
            "delete_procedure" => await DeleteProcedure(payload, cancellationToken),
            "list_events" => await ListEvents(payload, subject, cancellationToken),
            "get_event" => await Get(events, Id(payload), cancellationToken),
            "get_event_capabilities" => await GetEventCapabilities(payload, subject, cancellationToken),
            "get_event_write_permission" => await GetEventWritePermission(payload, subject, cancellationToken),
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
            "list_reminders" => await ListReminders(payload, cancellationToken),
            "get_reminder" => await GetReminder(payload, cancellationToken),
            "create_reminder" => await CreateReminder(payload, subject, cancellationToken),
            "update_reminder" => await UpdateReminder(payload, subject, cancellationToken),
            "delete_reminder" => await DeleteReminder(payload, cancellationToken),
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
        return new(callId, JsonSerializer.Serialize(result));
    }

    public SeaManagementPreparedAction? ParsePreparedAction(AgentToolResult result)
    {
        try { return JsonSerializer.Deserialize<SeaManagementPreparedAction>(result.Output); }
        catch (JsonException) { return null; }
    }

    public async Task<AgentToolResult> ExecutePreparedAsync(
        AgentToolCall call,
        SeaManagementPreparedAction action,
        SeaManagementAgentSubject subject,
        CancellationToken cancellationToken = default)
    {
        if (!IsMutation(action.Operation))
        {
            throw new InvalidOperationException("Only SEA mutations can be confirmed.");
        }

        if (!Guid.TryParse(subject.UserId, out Guid userId))
        {
            throw new UnauthorizedAccessException("The authenticated user identity is required.");
        }

        if (!await access.IsAuthorizedAsync(subject, action.Operation, action.PayloadJson, cancellationToken))
        {
            throw new UnauthorizedAccessException("The SEA operation is not permitted.");
        }

        using JsonDocument payloadDocument = JsonDocument.Parse(action.PayloadJson);
        return await ExecuteOperationAsync(call.Id, action.Operation, payloadDocument.RootElement.Clone(), new AgentExecutionContext(call.Id, new Dictionary<string, string> { ["authenticatedUserId"] = userId.ToString("D"), ["tenantId"] = subject.TenantId }), subject, cancellationToken);
    }

    private static bool IsMutation(string operation) => operation switch
    {
        "create_project" or "update_project" or "delete_project" or
        "create_procedure" or "update_procedure" or "delete_procedure" or
        "create_event" or "update_event" or "delete_event" or
        "move_event_up" or "move_event_down" or "repair_event_chain" or
        "commit_event" or "commit_procedure_events" or "uncommit_event" or "uncommit_procedure_events" or
        "create_reminder" or "update_reminder" or "delete_reminder" or
        "create_attachment" or "create_attachment_batch" or "update_attachment" or "delete_attachment" or
        "mark_read" or "mark_unread" or "create_role" or "update_role" or "delete_role" or
        "assign_user_role" or "remove_user_role" or "assign_event_role" or "remove_event_role" => true,
        _ => false
    };

    private async Task<object> ListProjects(JsonElement payload, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        IReadOnlyList<ProjectReadDto> values = (await projects.GetListAsync(Read<ProjectDescriptorDto>(payload), ct)).Value ?? [];
        List<ProjectReadDto> authorized = [];
        foreach (ProjectReadDto value in values)
        {
            if (await access.CanReadProjectAsync(subject, value.Id, ct))
            {
                authorized.Add(value);
            }
        }

        return authorized;
    }
    private async Task<object> ListProcedures(JsonElement payload, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        IReadOnlyList<ProcedureReadDto> values = (await procedures.GetListAsync(Read<ProcedureDescriptorDto>(payload), ct)).Value ?? [];
        List<ProcedureReadDto> authorized = [];
        foreach (ProcedureReadDto value in values)
        {
            if (await access.CanReadProcedureAsync(subject, value.Id, ct))
            {
                authorized.Add(value);
            }
        }

        return authorized;
    }
    private async Task<object> ListEvents(JsonElement payload, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        IReadOnlyList<EventReadDto> values = await events.GetByProcedureAsync(Id(payload), ct);
        List<EventReadDto> authorized = [];
        foreach (EventReadDto value in values)
        {
            if (await access.CanReadEventAsync(subject, value.Id, ct))
            {
                authorized.Add(value);
            }
        }

        return authorized;
    }
    private async Task<object> GetEventCapabilities(JsonElement p, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        Guid eventId = Id(p);
        return new { CanRead = await access.CanReadEventAsync(subject, eventId, ct), CanWrite = await access.CanWriteEventAsync(subject, eventId, ct), CanManage = await access.IsAuthorizedAsync(subject, "get_statistics", "{}", ct) };
    }
    private async Task<object> GetEventWritePermission(JsonElement p, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        return await access.CanWriteEventAsync(subject, Id(p), ct);
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
    private async Task<object> ListReminders(JsonElement p, CancellationToken ct) => reminders is null ? throw new InvalidOperationException("Reminder operations are unavailable.") : await reminders.GetBySourceAsync(Id(p), ct);
    private async Task<object> GetReminder(JsonElement p, CancellationToken ct)
    {
        if (reminders is null)
        {
            throw new InvalidOperationException("Reminder operations are unavailable.");
        }

        ReminderReadDto? reminder = (await reminders.GetAsync(Id(p), ct)).Value;
        return reminder is not null && reminder.SourceId == GuidValue(p, "eventId") ? reminder : throw new InvalidOperationException("Reminder was not found.");
    }
    private async Task<object> CreateReminder(JsonElement p, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        if (reminders is null)
        {
            throw new InvalidOperationException("Reminder operations are unavailable.");
        }

        Guid eventId = GuidValue(p, "sourceId");
        string recipientUserId = RequiredString(p, "recipientUserId");
        (ReminderKind kind, string recipient, string variables) = await ResolveReminderTarget(p, subject, eventId, recipientUserId, ct);
        DateTime remindAt = RequiredOffsetUtc(p, "remindAt");
        IReadOnlyList<ReminderReadDto> created = await reminders.AddRangeAsync(eventId, [new ReminderRecipientReadDto { Kind = kind, RecipientUserId = recipientUserId, Recipient = recipient }], RequiredString(p, "subject"), RequiredString(p, "textBody"), remindAt, "SeaReminder", "event-reminder", variables, ct);
        return created.SingleOrDefault() ?? throw new InvalidOperationException("Reminder could not be created.");
    }
    private async Task<object> UpdateReminder(JsonElement p, SeaManagementAgentSubject subject, CancellationToken ct)
    {
        if (reminders is null)
        {
            throw new InvalidOperationException("Reminder operations are unavailable.");
        }

        Guid reminderId = Id(p);
        ReminderReadDto existing = await GetUnsentReminder(reminderId, reminders, ct);
        Guid eventId = GuidValue(p, "eventId");
        if (existing.SourceId != eventId)
        {
            throw new UnauthorizedAccessException("The reminder is not attached to the requested event.");
        }

        string recipientUserId = RequiredString(p, "recipientUserId");
        (ReminderKind kind, string recipient, string variables) = await ResolveReminderTarget(p, subject, eventId, recipientUserId, ct);
        DateTime remindAt = RequiredOffsetUtc(p, "remindAt");
        ReminderReadDto? updated = await reminders.UpdateAsync(reminderId, kind, recipientUserId, recipient, RequiredString(p, "subject"), RequiredString(p, "textBody"), remindAt, "SeaReminder", "event-reminder", variables, ct);
        return updated ?? throw new InvalidOperationException("Reminder could not be updated.");
    }
    private async Task<object> DeleteReminder(JsonElement p, CancellationToken ct)
    {
        if (reminders is null)
        {
            throw new InvalidOperationException("Reminder operations are unavailable.");
        }

        ReminderReadDto existing = await GetUnsentReminder(Id(p), reminders, ct);
        if (existing.SourceId != GuidValue(p, "eventId"))
        {
            throw new UnauthorizedAccessException("The reminder is not attached to the requested event.");
        }

        await reminders.RemoveAsync(Id(p), ct);
        return true;
    }

    private async Task<(ReminderKind Kind, string Recipient, string Variables)> ResolveReminderTarget(JsonElement payload, SeaManagementAgentSubject subject, Guid eventId, string recipientUserId, CancellationToken ct)
    {
        if (users is null)
        {
            throw new InvalidOperationException("Reminder recipient validation is unavailable.");
        }

        if (!Guid.TryParse(recipientUserId, out Guid recipientId))
        {
            throw new JsonException("recipientUserId must be a valid GUID.");
        }

        bool tenantMember = await users.CheckUserRoleAsync(recipientUserId, $"{subject.TenantId}:TenantUser", ct) ||
            await users.CheckUserRoleAsync(recipientUserId, $"{subject.TenantId}:TenantOwner", ct) ||
            await users.CheckUserRoleAsync(recipientUserId, IdentityDefinitions.PlatformAdmin, ct);
        if (!tenantMember)
        {
            throw new UnauthorizedAccessException("The reminder recipient is not a user of the tenant.");
        }

        SeaManagementAgentSubject recipientSubject = subject with { UserId = recipientId.ToString("D"), EventId = null };
        if (!await access.CanReadEventAsync(recipientSubject, eventId, ct))
        {
            throw new UnauthorizedAccessException("The reminder recipient cannot read the event.");
        }

        Vitarish.Users.Domain.Entities.User? user = await users.GetUserAsync(recipientUserId, ct);
        if (user is null)
        {
            throw new UnauthorizedAccessException("The reminder recipient was not found.");
        }

        ReminderKind kind = Enum.Parse<ReminderKind>(RequiredString(payload, "kind"), true);
        string recipient = kind switch
        {
            ReminderKind.Email when user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Email) => user.Email,
            ReminderKind.Sms when user.PhoneNumberConfirmed && !string.IsNullOrWhiteSpace(user.PhoneNumber) => user.PhoneNumber,
            _ => throw new ArgumentException("The recipient must have a verified registered contact for the selected reminder kind.")
        };
        EventReadDto ev = (await events.GetAsync(eventId, ct)).Value ?? throw new InvalidOperationException("SEA event was not found.");
        ProcedureReadDto procedure = (await procedures.GetAsync(ev.ProcedureId, ct)).Value ?? throw new InvalidOperationException("SEA procedure was not found.");
        ProjectReadDto project = (await projects.GetAsync(ev.ProjectId, ct)).Value ?? throw new InvalidOperationException("SEA project was not found.");
        string variables = JsonSerializer.Serialize(new
        {
            tenantId = subject.TenantId,
            projectId = project.Id,
            projectName = project.Name,
            procedureId = procedure.Id,
            procedureName = procedure.Name,
            eventId = ev.Id,
            eventTitle = ev.Title
        });
        return (kind, recipient, variables);
    }

    private static async Task<ReminderReadDto> GetUnsentReminder(Guid id, IRemindersService service, CancellationToken ct)
    {
        ReminderReadDto? reminder = (await service.GetAsync(id, ct)).Value;
        if (reminder is null)
        {
            throw new InvalidOperationException("Reminder was not found.");
        }

        if (reminder.IsSent || reminder.SentAt is not null)
        {
            throw new InvalidOperationException("Sent reminders are immutable.");
        }

        return reminder;
    }
    private async Task<object> CreateAttachment(JsonElement p, CancellationToken ct) =>
        (await attachments.AddAsync(GuidValue(p, "eventId"), Content(p), RequiredString(p, "contentType"), OptionalString(p, "contentThumbnail"), ValidatedSize(p), OptionalString(p, "fileName"), ValidatedKind(p), ct)).Value
        ?? throw new InvalidOperationException("SEA attachment could not be created.");
    private async Task<object> CreateAttachmentBatch(JsonElement p, CancellationToken ct)
    {
        JsonElement items = p.GetProperty("items");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 100)
        {
            throw new JsonException("items must contain between 1 and 100 attachments.");
        }

        IReadOnlyList<AttachmentWriteDto> values = JsonSerializer.Deserialize<IReadOnlyList<AttachmentWriteDto>>(items.GetRawText(), PayloadJsonOptions) ?? throw new JsonException("items is invalid.");
        foreach (AttachmentWriteDto value in values)
        {
            ValidateAttachmentFields(value.Content, value.ContentType, value.Size, value.Kind, value.FileName);
        }

        return (await attachments.AddBatchAsync(values, ct)).Value ?? throw new InvalidOperationException("SEA attachments could not be created.");
    }
    private async Task<object> UpdateAttachment(JsonElement p, CancellationToken ct) => await attachments.UpdateAsync(GuidValue(p, "eventId"), Id(p), Content(p), ct) ?? throw new InvalidOperationException("SEA attachment could not be updated.");
    private async Task<object> DeleteAttachment(JsonElement p, CancellationToken ct) { await attachments.RemoveAsync(GuidValue(p, "eventId"), Id(p), ct); return true; }
    private async Task<object> MarkRead(JsonElement p, bool read, CancellationToken ct)
    {
        Guid userId = GuidValue(p, "userId");
        Guid objectId = Id(p);
        string kind = RequiredString(p, "kind");
        if (kind == "project") { if (read)
            {
                await receipts.MarkProjectReadAsync(userId, objectId, ct);
            }
            else
            {
                await receipts.MarkProjectUnreadAsync(userId, objectId, ct);
            }
        }
        else if (kind == "procedure") { if (read)
            {
                await receipts.MarkProcedureReadAsync(userId, objectId, ct);
            }
            else
            {
                await receipts.MarkProcedureUnreadAsync(userId, objectId, ct);
            }
        }
        else if (kind == "event") { if (read)
            {
                await receipts.MarkEventReadAsync(userId, objectId, ct);
            }
            else
            {
                await receipts.MarkEventUnreadAsync(userId, objectId, ct);
            }
        }
        else
        {
            throw new JsonException("kind must be project, procedure, or event.");
        }

        return true;
    }
    private async Task<object> UpdateRole(JsonElement p, CancellationToken ct) { await roles.UpdateAsync(Id(p), RequiredString(p, "name"), ct); return true; }
    private async Task<object> AssignUserRole(JsonElement p, CancellationToken ct) { await userRoles.AssignAsync(GuidValue(p, "userId"), GuidValue(p, "roleId"), Enum.Parse<RoleType>(RequiredString(p, "type"), true), ct); return true; }
    private async Task<object> RemoveUserRole(JsonElement p, CancellationToken ct) { await userRoles.RemoveAsync(GuidValue(p, "userId"), GuidValue(p, "roleId"), ct); return true; }
    private async Task<object> AssignEventRole(JsonElement p, bool assign, CancellationToken ct) { if (assign)
        {
            await eventRoles.AssignAsync(Id(p), GuidValue(p, "roleId"), p.GetProperty("isWrite").GetBoolean(), ct);
        }
        else
        {
            await eventRoles.UnassignAsync(Id(p), GuidValue(p, "roleId"), p.GetProperty("isWrite").GetBoolean(), ct);
        }
        return true; }

    private static T Read<T>(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Activator.CreateInstance<T>() : JsonSerializer.Deserialize<T>(value.GetRawText()) ?? throw new JsonException("payload is invalid.");
    private static Guid Id(JsonElement p) => GuidValue(p, "id");
    private static Guid AuthenticatedUserId(AgentExecutionContext context) =>
        context.Metadata is not null && context.Metadata.TryGetValue("authenticatedUserId", out string? value) && Guid.TryParse(value, out Guid id)
            ? id
            : throw new UnauthorizedAccessException("The authenticated user identity is required.");
    private static SeaManagementAgentSubject Subject(AgentExecutionContext context)
    {
        IReadOnlyDictionary<string, string> metadata = context.Metadata ?? throw new UnauthorizedAccessException("The SEA scope is required.");
        string tenantId = metadata.TryGetValue("tenantId", out string? tenant) ? tenant : metadata.TryGetValue("practiceId", out string? practice) ? practice : string.Empty;
        string userId = metadata.TryGetValue("authenticatedUserId", out string? user) ? user : string.Empty;
        Guid? eventId = metadata.TryGetValue("subjectKind", out string? subjectKind) && string.Equals(subjectKind, "event", StringComparison.Ordinal) &&
            metadata.TryGetValue("subjectId", out string? subjectId) && Guid.TryParse(subjectId, out Guid parsedEventId)
            ? parsedEventId : null;
        return new(tenantId, userId, eventId);
    }
    private static Guid GuidValue(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.TryGetGuid(out Guid id) ? id : throw new JsonException($"{name} must be a valid GUID.");
    private static Guid? OptionalGuid(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null && value.TryGetGuid(out Guid id) ? id : null;
    private static string RequiredString(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? text : throw new JsonException($"{name} is required.");
    private static string? OptionalString(JsonElement p, string name) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string Content(JsonElement p) { string value = RequiredString(p, "content"); return value.Length <= MaxContentLength ? value : throw new JsonException("content is too large."); }
    private static long ValidatedSize(JsonElement p)
    {
        if (!p.TryGetProperty("size", out JsonElement value) || !value.TryGetInt64(out long size))
        {
            throw new JsonException("size must be a valid integer.");
        }

        return size >= 0 ? size : throw new JsonException("size must not be negative.");
    }
    private static AttachmentKind ValidatedKind(JsonElement p)
    {
        AttachmentKind kind;
        try { kind = Enum.Parse<AttachmentKind>(RequiredString(p, "kind"), true); }
        catch (ArgumentException) { throw new JsonException("kind must be a supported attachment kind."); }
        ValidateAttachmentFields(Content(p), RequiredString(p, "contentType"), ValidatedSize(p), kind, OptionalString(p, "fileName"));
        return kind;
    }
    private static void ValidateAttachmentFields(string content, string contentType, long size, AttachmentKind kind, string? fileName)
    {
        if (content.Length is 0 or > MaxContentLength)
        {
            throw new JsonException("content is invalid or too large.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new JsonException("contentType is required.");
        }

        if (size < 0)
        {
            throw new JsonException("size must not be negative.");
        }

        EntityResult validation = Attachment.ValidateNew(kind, fileName);
        if (validation.IsFailure)
        {
            throw new JsonException(validation.Errors.FirstOrDefault()?.Message ?? "attachment fields are invalid.");
        }
    }
    private static DateTime RequiredOffsetUtc(JsonElement p, string name)
    {
        string value = RequiredString(p, name);
        if (!value.EndsWith('Z') && !value.Contains('+') && value.LastIndexOf('-') <= 10)
        {
            throw new JsonException($"{name} must include an ISO 8601 offset.");
        }

        return DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed.UtcDateTime : throw new JsonException($"{name} must be a valid ISO 8601 timestamp.");
    }
}
