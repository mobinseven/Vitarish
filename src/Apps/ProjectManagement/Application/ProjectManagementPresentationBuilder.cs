using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.ProjectManagement.Application;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProjectManagementTextPart), "text")]
[JsonDerivedType(typeof(ProjectManagementResourceCardPart), "resource-card")]
public abstract record ProjectManagementDisplayPart;

public sealed record ProjectManagementTextPart(string Text) : ProjectManagementDisplayPart;

public sealed record ProjectManagementResourceCardPart(
    string Family,
    string Operation,
    string? Id,
    string? ParentId,
    string RouteKind,
    IReadOnlyDictionary<string, string> RouteCoordinates,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, JsonElement> Fields,
    IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Items) : ProjectManagementDisplayPart;

public sealed record ProjectManagementPresentationResult(
    string AssistantText,
    IReadOnlyList<ProjectManagementDisplayPart> Parts,
    JsonElement PersistedParts);

/// <summary>Turns model references into snapshots only after matching successful, authorized SEA reads.</summary>
public sealed class ProjectManagementPresentationBuilder
{
    private const int MaximumPreviewBytes = 4096;
    private const int MaximumPresentationBytes = 1024 * 1024;
    private static readonly HashSet<string> ReadOperations = new(StringComparer.Ordinal)
    {
        "health", "get_capabilities", "list_projects", "get_project", "get_project_snapshot",
        "list_procedures", "get_procedure", "list_events", "get_event", "get_event_capabilities",
        "get_event_write_permission", "get_event_reminders_count", "list_reminders", "get_reminder",
        "list_attachments", "count_attachments", "list_roles", "list_user_roles", "list_event_roles",
        "get_event_read_roles", "get_event_write_roles", "get_statistics"
    };

    private static readonly HashSet<string> Mutations = new(StringComparer.Ordinal)
    {
        "create_project", "update_project", "delete_project", "create_procedure", "update_procedure", "delete_procedure",
        "create_event", "update_event", "delete_event", "move_event_up", "move_event_down", "repair_event_chain",
        "commit_event", "commit_procedure_events", "uncommit_event", "uncommit_procedure_events", "create_reminder",
        "update_reminder", "delete_reminder", "create_attachment", "create_attachment_batch", "update_attachment",
        "delete_attachment", "mark_read", "mark_unread", "create_role", "update_role", "delete_role",
        "assign_user_role", "remove_user_role", "assign_event_role", "remove_event_role"
    };

    public ProjectManagementPresentationResult Build(JsonElement? structuredOutput, IReadOnlyList<AgentToolExecution> executions, ProjectManagementAgentContext context, DateTimeOffset capturedAt)
    {
        if (structuredOutput is not { ValueKind: JsonValueKind.Object } root)
        {
            throw new JsonException("The project-management result is not an object.");
        }

        JsonElement supplied = root.TryGetProperty("parts", out JsonElement parts) ? parts : root.TryGetProperty("messages", out JsonElement messages) ? messages : default;
        if (supplied.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The project-management result parts are invalid.");
        }

        List<ProjectManagementDisplayPart> display = [];
        foreach (JsonElement part in supplied.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                string? text = part.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    display.Add(new ProjectManagementTextPart(text));
                }

                continue;
            }
            if (part.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("A presentation part is invalid.");
            }

            if (part.TryGetProperty("text", out JsonElement textValue) && textValue.ValueKind == JsonValueKind.String)
            {
                string? text = textValue.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    display.Add(new ProjectManagementTextPart(text));
                }

                continue;
            }
            if (!part.TryGetProperty("type", out _) && (part.TryGetProperty("content", out JsonElement content) || part.TryGetProperty("message", out content)) && content.ValueKind == JsonValueKind.String)
            {
                string? text = content.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    display.Add(new ProjectManagementTextPart(text));
                }

                continue;
            }
            if (!part.TryGetProperty("type", out JsonElement type) || type.ValueKind != JsonValueKind.String || type.GetString() != "sea-reference")
            {
                throw new JsonException("Only text and SEA references are supported.");
            }

            string operation = RequiredString(part, "operation");
            string? scopeId = OptionalString(part, "scopeId");
            string? itemId = OptionalString(part, "itemId");
            AgentToolExecution execution = FindExecution(operation, scopeId, itemId, executions);
            display.Add(BuildCard(operation, scopeId, itemId, execution, executions, context, capturedAt));
        }
        IReadOnlyList<ProjectManagementDisplayPart> immutable = display.ToArray();
        string assistantText = string.Join("\n\n", immutable.OfType<ProjectManagementTextPart>().Select(x => x.Text));
        JsonElement persisted = JsonSerializer.SerializeToElement(immutable);
        if (Encoding.UTF8.GetByteCount(persisted.GetRawText()) > MaximumPresentationBytes)
        {
            throw new InvalidOperationException("The project-management presentation is too large.");
        }

        return new(assistantText, immutable, persisted);
    }

    private static AgentToolExecution FindExecution(string operation, string? scopeId, string? itemId, IReadOnlyList<AgentToolExecution> executions)
    {
        if (Mutations.Contains(operation) || !ReadOperations.Contains(operation))
        {
            throw new UnauthorizedAccessException("The referenced SEA operation is not presentable.");
        }

        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError || !string.Equals(execution.ToolName, "sea_manage", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(execution.ArgumentsJson))
            {
                continue;
            }

            try
            {
                using JsonDocument args = JsonDocument.Parse(execution.ArgumentsJson);
                if (!args.RootElement.TryGetProperty("operation", out JsonElement supplied) || supplied.GetString() != operation)
                {
                    continue;
                }

                JsonElement payload = args.RootElement.TryGetProperty("payload", out JsonElement value) ? value : default;
                if (scopeId is not null && !ContainsId(payload, scopeId))
                {
                    continue;
                }

                return execution;
            }
            catch (JsonException) { continue; }
        }
        throw new UnauthorizedAccessException("The referenced SEA read was not executed successfully in this turn.");
    }

    private static ProjectManagementResourceCardPart BuildCard(string operation, string? scopeId, string? itemId, AgentToolExecution execution, IReadOnlyList<AgentToolExecution> executions, ProjectManagementAgentContext context, DateTimeOffset capturedAt)
    {
        using JsonDocument output = JsonDocument.Parse(execution.Output);
        JsonElement value = output.RootElement;
        JsonElement selected = SelectItem(value, itemId);
        string? parentId = ParentId(operation, selected, scopeId, itemId);
        if ((operation is "list_attachments" or "count_attachments" or "list_reminders" or "get_reminder") && !Guid.TryParse(parentId, out _))
        {
            throw new UnauthorizedAccessException("A parent event is required for this SEA display.");
        }

        string family = Family(operation);
        IReadOnlyDictionary<string, JsonElement> fields = SafeFields(selected, family);
        IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> items = selected.ValueKind == JsonValueKind.Array
            ? selected.EnumerateArray().Select(item => SafeFields(item, family)).ToArray()
            : [];
        if (selected.ValueKind == JsonValueKind.Array && itemId is not null && items.Count == 0)
        {
            throw new JsonException("The selected SEA item was not found.");
        }

        string? id = itemId ?? StringId(selected) ?? scopeId;
        if (context.EventId is { } anchoredEvent)
        {
            string eventId = anchoredEvent.ToString("D");
            if ((family == "event" && id != eventId) || (family is "attachment" or "reminder" && parentId != eventId))
            {
                throw new UnauthorizedAccessException("The referenced SEA resource is outside the current event.");
            }
        }
        string routeKind = family switch { "project" => "project", "procedure" => "procedure", "event" => "event", "attachment" or "reminder" => "event", "role" => "roles", "statistics" => "statistics", _ => "status" };
        if (family is "attachment" or "reminder" && !Guid.TryParse(parentId, out _))
        {
            throw new UnauthorizedAccessException("The SEA parent route is not verified.");
        }

        Dictionary<string, string> coordinates = new(StringComparer.Ordinal);
        CopyCoordinate(selected, coordinates, "projectId");
        CopyCoordinate(selected, coordinates, "procedureId");
        CopyCoordinate(selected, coordinates, "eventId");
        if (family is "attachment" or "reminder")
        {
            coordinates["eventId"] = parentId!;
            JsonElement? parentEvent = FindOutput(executions, "get_event", parentId!);
            if (parentEvent is not { } eventValue)
            {
                throw new UnauthorizedAccessException("The attachment or reminder parent event was not verified in this turn.");
            }

            CopyCoordinate(eventValue, coordinates, "projectId"); CopyCoordinate(eventValue, coordinates, "procedureId");
            if (!coordinates.ContainsKey("projectId") || !coordinates.ContainsKey("procedureId"))
            {
                throw new JsonException("The parent event route coordinates are incomplete.");
            }
        }
        return new(family, operation, id, parentId, routeKind, coordinates, capturedAt, fields, items);
    }

    private static string Family(string operation) => operation switch
    {
        "health" or "get_capabilities" => "status",
        "list_projects" or "get_project" or "get_project_snapshot" => "project",
        "list_procedures" or "get_procedure" => "procedure",
        "list_events" or "get_event" or "get_event_capabilities" or "get_event_write_permission" => "event",
        "get_event_reminders_count" or "list_reminders" or "get_reminder" => "reminder",
        "list_attachments" or "count_attachments" => "attachment",
        "list_roles" or "list_user_roles" or "list_event_roles" or "get_event_read_roles" or "get_event_write_roles" => "role",
        "get_statistics" => "statistics",
        _ => throw new JsonException("The SEA operation has no display mapping.")
    };

    private static JsonElement SelectItem(JsonElement value, string? itemId)
    {
        if (itemId is null || value.ValueKind != JsonValueKind.Array)
        {
            return value;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (StringId(item) == itemId)
            {
                return item;
            }
        }

        throw new JsonException("The selected SEA item was not returned by the read.");
    }

    private static IReadOnlyDictionary<string, JsonElement> SafeFields(JsonElement value, string family)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, JsonElement>();
        }

        string[] allowed = family switch
        {
            "attachment" => ["id", "eventId", "fileName", "contentType", "size", "kind", "createdAt", "updatedAt"],
            "reminder" => ["id", "sourceId", "kind", "subject", "textBody", "remindAt", "isSent", "createdAt"],
            "role" => ["id", "name", "type", "userId", "roleId", "eventId", "isWrite"],
            "statistics" => ["projects", "procedures", "events", "roles", "attachments", "reminders"],
            "status" => ["succeeded", "canManage", "canRead", "canWrite"],
            _ => ["id", "projectId", "procedureId", "previousEventId", "name", "title", "description", "isCommitted", "createdAt", "updatedAt"]
        };
        Dictionary<string, JsonElement> result = new(StringComparer.Ordinal);
        foreach (string name in allowed)
        {
            if (value.TryGetProperty(name, out JsonElement item))
            {
                result[name] = name == "content" ? Preview(item) : item.Clone();
            }
        }

        if (family == "attachment" && value.TryGetProperty("content", out JsonElement content) && StringProperty(value, "contentType") is { } contentType && contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            result["preview"] = Preview(content);
        }

        return result;
    }

    private static JsonElement Preview(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return JsonDocument.Parse("null").RootElement.Clone();
        }

        string text = value.GetString() ?? string.Empty;
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length <= MaximumPreviewBytes)
        {
            return JsonSerializer.SerializeToElement(new { text, truncated = false });
        }

        return JsonSerializer.SerializeToElement(new { text = Encoding.UTF8.GetString(bytes, 0, MaximumPreviewBytes), truncated = true });
    }

    private static string? ParentId(string operation, JsonElement value, string? scopeId, string? itemId) => operation switch
    {
        "list_attachments" or "count_attachments" or "get_event_reminders_count" or "list_reminders" => scopeId ?? StringProperty(value, "eventId") ?? StringProperty(value, "sourceId"),
        "get_reminder" => StringProperty(value, "sourceId") ?? scopeId,
        "list_events" => scopeId ?? StringProperty(value, "procedureId"),
        "list_procedures" => scopeId ?? StringProperty(value, "projectId"),
        _ => StringProperty(value, "procedureId") ?? StringProperty(value, "projectId")
    };

    private static bool ContainsId(JsonElement value, string id) => StringId(value) == id ||
        (value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Any(property => property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() == id));
    private static string? StringId(JsonElement value) => StringProperty(value, "id");
    private static string? StringProperty(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static void CopyCoordinate(JsonElement value, IDictionary<string, string> target, string name) { if (StringProperty(value, name) is { Length: > 0 } coordinate)
        {
            target[name] = coordinate;
        }
    }
    private static JsonElement? FindOutput(IReadOnlyList<AgentToolExecution> executions, string operation, string id)
    {
        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError || string.IsNullOrWhiteSpace(execution.ArgumentsJson))
            {
                continue;
            }

            try
            {
                using JsonDocument args = JsonDocument.Parse(execution.ArgumentsJson);
                if (StringProperty(args.RootElement, "operation") != operation)
                {
                    continue;
                }

                using JsonDocument output = JsonDocument.Parse(execution.Output);
                if (StringId(output.RootElement) == id)
                {
                    return output.RootElement.Clone();
                }
            }
            catch (JsonException) { }
        }
        return null;
    }
    private static string RequiredString(JsonElement value, string name) => StringProperty(value, name) is { Length: > 0 } text ? text : throw new JsonException($"{name} is required.");
    private static string? OptionalString(JsonElement value, string name) => StringProperty(value, name);
}
