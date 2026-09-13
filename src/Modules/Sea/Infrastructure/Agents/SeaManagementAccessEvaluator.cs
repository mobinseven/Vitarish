using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Agents;

/// <summary>Owns the SEA agent permission decision; callers cannot grant access with tool arguments.</summary>
public sealed class SeaManagementAccessEvaluator(
    IUsersService users,
    IUserRolesService userRoles,
    IEventRolesService eventRoles,
    IEventsService events,
    SeaDbContext dbContext) : ISeaManagementAccessEvaluator
{
    private readonly Dictionary<SubjectKey, bool> managementAccess = [];
    private readonly Dictionary<Guid, RoleAssignments> userRoleAssignments = [];
    private readonly Dictionary<Guid, EventRoleAssignments> eventRoleAssignments = [];

    public Task<bool> CanManageAsync(SeaManagementAgentSubject subject, CancellationToken ct = default) =>
        CanManageCoreAsync(subject, ct);

    public async Task<bool> CanReadAttachmentAsync(SeaManagementAgentSubject subject, Guid attachmentId, CancellationToken ct = default)
    {
        Guid? eventId = await dbContext.Attachments
            .Where(attachment => attachment.Id == attachmentId)
            .Select(attachment => (Guid?)attachment.EventId)
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return eventId is { } id && await CanReadEventAsync(subject, id, ct).ConfigureAwait(false);
    }

    public async Task<bool> CanWriteAttachmentAsync(SeaManagementAgentSubject subject, Guid attachmentId, CancellationToken ct = default)
    {
        Guid? eventId = await dbContext.Attachments
            .Where(attachment => attachment.Id == attachmentId)
            .Select(attachment => (Guid?)attachment.EventId)
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return eventId is { } id && await CanWriteEventAsync(subject, id, ct).ConfigureAwait(false);
    }

    public async Task PrimeEventRolesAsync(IEnumerable<Guid> eventIds, CancellationToken ct = default)
    {
        Guid[] missing = eventIds.Distinct().Where(eventId => !eventRoleAssignments.ContainsKey(eventId)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> loaded = await eventRoles.GetIdsForEventsAsync(missing, ct);
        foreach (Guid eventId in missing)
        {
            (IReadOnlyList<Guid> read, IReadOnlyList<Guid> write) = loaded.TryGetValue(eventId, out var roles) ? roles : ([], []);
            eventRoleAssignments[eventId] = new(read, write);
        }
    }

    public async Task<bool> CanAccessTenantWorkspaceAsync(SeaManagementAgentSubject subject, CancellationToken ct = default)
    {
        if (await CanManageCoreAsync(subject, ct))
        {
            return true;
        }

        IReadOnlyList<EventReadDto> candidateEvents = (await events.GetListAsync(new EventDescriptorDto(), ct)).Value ?? [];
        foreach (EventReadDto candidate in candidateEvents)
        {
            if (await CanWriteEventAsync(subject, candidate.Id, ct))
            {
                return true;
            }
        }

        return false;
    }
    public async Task<bool> IsAuthorizedAsync(SeaManagementAgentSubject subject, string operation, string payloadJson, CancellationToken ct = default)
    {
        if (!IsValid(subject, operation))
        {
            return false;
        }

        if (subject.EventId is { } eventId)
        {
            return await IsAuthorizedForEventAsync(subject, eventId, operation, payloadJson, ct);
        }

        if (await CanManageCoreAsync(subject, ct))
        {
            return true;
        }

        if (operation is "health" or "get_capabilities" or "list_projects" or "list_procedures" or "list_events")
        {
            return true;
        }

        using JsonDocument document = Parse(payloadJson);
        JsonElement p = document.RootElement;
        return operation switch
        {
            "get_project" or "get_project_snapshot" => await CanReadProjectAsync(subject, Id(p), ct),
            "get_procedure" => await CanReadProcedureAsync(subject, Id(p), ct),
            "get_event" or "get_event_capabilities" or "get_event_write_permission" or "get_event_reminders_count" or "list_attachments" or "count_attachments" or "list_reminders" => await CanReadEventAsync(subject, Id(p), ct),
            "get_reminder" => await CanReadEventAsync(subject, GuidValue(p, "eventId"), ct),
            "create_reminder" => await CanWriteEventAsync(subject, GuidValue(p, "sourceId"), ct),
            "update_reminder" or "delete_reminder" => await CanWriteEventAsync(subject, GuidValue(p, "eventId"), ct),
            "mark_read" or "mark_unread" => await CanMutateReceiptAsync(subject, p, ct),
            "update_event" or "delete_event" or "move_event_up" or "move_event_down" or "commit_event" or "uncommit_event" => await CanWriteEventAsync(subject, Id(p), ct),
            "create_attachment" or "update_attachment" or "delete_attachment" => await CanWriteEventAsync(subject, GuidValue(p, "eventId"), ct),
            "create_attachment_batch" => await CanWriteAttachmentBatchAsync(subject, p, ct),
            _ => false
        };
    }

    private async Task<bool> CanWriteAttachmentBatchAsync(SeaManagementAgentSubject subject, JsonElement payload, CancellationToken ct)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 100)
        {
            return false;
        }

        foreach (JsonElement item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("eventId", out JsonElement eventId) || !eventId.TryGetGuid(out Guid parsedEventId) ||
                !await CanWriteEventAsync(subject, parsedEventId, ct))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> IsAuthorizedForEventAsync(SeaManagementAgentSubject subject, Guid eventId, string operation, string payloadJson, CancellationToken ct)
    {
        using JsonDocument document = Parse(payloadJson);
        JsonElement p = document.RootElement;
        bool isCurrentEvent = operation switch
        {
            "get_event" or "list_attachments" or "count_attachments" or "get_event_reminders_count" or "list_reminders" or "update_event" or "delete_event" or "commit_event" or "uncommit_event" => Id(p) == eventId,
            "get_reminder" => GuidValue(p, "eventId") == eventId,
            "create_reminder" => GuidValue(p, "sourceId") == eventId,
            "update_reminder" or "delete_reminder" => GuidValue(p, "eventId") == eventId,
            "create_attachment" => GuidValue(p, "eventId") == eventId,
            "create_attachment_batch" => await IsAttachmentBatchForEventAsync(subject, p, eventId, ct),
            "update_attachment" or "delete_attachment" => GuidValue(p, "eventId") == eventId && await IsAttachmentOfEventAsync(Id(p), eventId, ct),
            "mark_read" or "mark_unread" => RequiredString(p, "kind") == "event" && Id(p) == eventId && GuidValue(p, "userId") == Guid.Parse(subject.UserId),
            _ => false
        };
        if (!isCurrentEvent)
        {
            return false;
        }

        return operation is "get_event" or "list_attachments" or "count_attachments" or "get_event_reminders_count" or "list_reminders" or "get_reminder"
            ? await CanReadEventAsync(subject with { EventId = null }, eventId, ct)
            : await CanWriteEventAsync(subject with { EventId = null }, eventId, ct);
    }

    private Task<bool> IsAttachmentOfEventAsync(Guid attachmentId, Guid eventId, CancellationToken ct) =>
        dbContext.Attachments.AnyAsync(attachment => attachment.Id == attachmentId && attachment.EventId == eventId, ct);

    private static bool IsAttachmentBatchForEvent(JsonElement payload, Guid eventId)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() is 0 or > 100)
        {
            return false;
        }

        return items.EnumerateArray().All(item => item.TryGetProperty("eventId", out JsonElement value) && value.TryGetGuid(out Guid parsed) && parsed == eventId);
    }

    private async Task<bool> IsAttachmentBatchForEventAsync(SeaManagementAgentSubject subject, JsonElement payload, Guid eventId, CancellationToken ct) =>
        IsAttachmentBatchForEvent(payload, eventId) && await CanWriteEventAsync(subject with { EventId = null }, eventId, ct);

    public async Task<bool> CanReadProjectAsync(SeaManagementAgentSubject subject, Guid projectId, CancellationToken ct = default) =>
        await CanManageCoreAsync(subject, ct) || await HasReadableEventAsync(subject, new EventDescriptorDto { ProjectId = projectId }, ct);

    public async Task<bool> CanReadProcedureAsync(SeaManagementAgentSubject subject, Guid procedureId, CancellationToken ct = default) =>
        await CanManageCoreAsync(subject, ct) || await HasReadableEventAsync(subject, new EventDescriptorDto { ProcedureId = procedureId }, ct);

    public async Task<bool> CanReadEventAsync(SeaManagementAgentSubject subject, Guid eventId, CancellationToken ct = default) =>
        await CanManageCoreAsync(subject, ct) || await HasEventAccessAsync(subject, eventId, false, ct);

    public async Task<bool> CanWriteEventAsync(SeaManagementAgentSubject subject, Guid eventId, CancellationToken ct = default) =>
        await CanManageCoreAsync(subject, ct) || await HasEventAccessAsync(subject, eventId, true, ct);

    private async Task<bool> CanMutateReceiptAsync(SeaManagementAgentSubject subject, JsonElement p, CancellationToken ct)
    {
        if (!Guid.TryParse(subject.UserId, out Guid userId) || GuidValue(p, "userId") != userId)
        {
            return false;
        }

        return RequiredString(p, "kind") switch
        {
            "project" => await CanReadProjectAsync(subject, Id(p), ct),
            "procedure" => await CanReadProcedureAsync(subject, Id(p), ct),
            "event" => await CanReadEventAsync(subject, Id(p), ct),
            _ => false
        };
    }

    private async Task<bool> HasReadableEventAsync(SeaManagementAgentSubject subject, EventDescriptorDto descriptor, CancellationToken ct)
    {
        IReadOnlyList<EventReadDto> candidates = (await events.GetListAsync(descriptor, ct)).Value ?? [];
        foreach (EventReadDto candidate in candidates)
        {
            if (await HasEventAccessAsync(subject, candidate.Id, false, ct))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> HasEventAccessAsync(SeaManagementAgentSubject subject, Guid eventId, bool write, CancellationToken ct)
    {
        if (!Guid.TryParse(subject.UserId, out Guid userId))
        {
            return false;
        }

        RoleAssignments assignments = await GetUserRoleAssignmentsAsync(userId, ct);
        EventRoleAssignments eventAssignments = await GetEventRoleAssignmentsAsync(eventId, ct);
        bool canWrite = eventAssignments.WriteRoleIds.Any(assignments.WriteRoleIds.Contains);
        if (write)
        {
            return canWrite;
        }

        return canWrite || eventAssignments.ReadRoleIds.Any(assignments.ReadRoleIds.Contains);
    }

    private async Task<RoleAssignments> GetUserRoleAssignmentsAsync(Guid userId, CancellationToken ct)
    {
        if (userRoleAssignments.TryGetValue(userId, out RoleAssignments? cached))
        {
            return cached;
        }

        IReadOnlyList<UserRoleReadDto> assignments = (await userRoles.GetListAsync(new UserRoleDescriptorDto { UserId = userId }, ct)).Value ?? [];
        HashSet<Guid> write = [.. assignments.Where(role => role.Type == RoleType.Write).Select(role => role.RoleId)];
        RoleAssignments result = new([.. assignments.Where(role => role.Type == RoleType.Read).Select(role => role.RoleId), .. write], write);
        userRoleAssignments[userId] = result;
        return result;
    }

    private async Task<EventRoleAssignments> GetEventRoleAssignmentsAsync(Guid eventId, CancellationToken ct)
    {
        if (eventRoleAssignments.TryGetValue(eventId, out EventRoleAssignments? cached))
        {
            return cached;
        }

        (IReadOnlyList<Guid> read, IReadOnlyList<Guid> write) = await eventRoles.GetIdsAsync(eventId, ct);
        EventRoleAssignments result = new(read, write);
        eventRoleAssignments[eventId] = result;
        return result;
    }

    private async Task<bool> CanManageCoreAsync(SeaManagementAgentSubject subject, CancellationToken ct)
    {
        SubjectKey key = new(subject.TenantId, subject.UserId);
        if (managementAccess.TryGetValue(key, out bool cached))
        {
            return cached;
        }

        bool result = await users.CheckUserRoleAsync(subject.UserId, IdentityDefinitions.PlatformAdmin, ct) ||
            await users.CheckUserRoleAsync(subject.UserId, $"{subject.TenantId}:TenantOwner", ct);
        managementAccess[key] = result;
        return result;
    }

    private sealed record RoleAssignments(HashSet<Guid> ReadRoleIds, HashSet<Guid> WriteRoleIds);
    private sealed record EventRoleAssignments(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds);
    private readonly record struct SubjectKey(string TenantId, string UserId);

    private static bool IsValid(SeaManagementAgentSubject subject, string operation) => !string.IsNullOrWhiteSpace(subject.TenantId) && Guid.TryParse(subject.UserId, out _) && !string.IsNullOrWhiteSpace(operation);
    private static JsonDocument Parse(string json) { try { return JsonDocument.Parse(json); } catch (JsonException) { throw new UnauthorizedAccessException("The SEA payload is invalid."); } }
    private static Guid Id(JsonElement value) => GuidValue(value, "id");
    private static Guid GuidValue(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out JsonElement id) && id.TryGetGuid(out Guid result) ? result : throw new UnauthorizedAccessException($"{name} is required.");
    private static string RequiredString(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out JsonElement result) && result.ValueKind == JsonValueKind.String && result.GetString() is { Length: > 0 } text ? text : throw new UnauthorizedAccessException($"{name} is required.");
}
