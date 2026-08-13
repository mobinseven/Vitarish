using System.Text.Json;
using System.Text.RegularExpressions;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;

public sealed class ReminderManagementAgentTool(IRemindersService reminders, IMessagesService messages, IReminderSourceRegistry sourceRegistry) : IClinicOperationsAgentTool, IAssistantPreparationTool
{
    private static readonly HashSet<string> Mutations = ["create_reminder", "update_reminder", "delete_reminder"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AgentToolDefinition Definition { get; } = new(
        "reminder_manage",
        "Inspect and safely manage one-off practice reminders. Mutations require explicit confirmation; sending is performed only by the background processor.",
        "{\"type\":\"object\",\"properties\":{\"operation\":{\"type\":\"string\",\"enum\":[\"list_reminders\",\"get_reminder\",\"list_reminders_by_source\",\"get_reminder_delivery_state\",\"create_reminder\",\"update_reminder\",\"delete_reminder\"]},\"payload\":{\"type\":\"object\"},\"commit\":{\"type\":\"boolean\"}},\"required\":[\"operation\"],\"additionalProperties\":false}");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement root = document.RootElement;
        string operation = RequiredString(root, "operation");
        JsonElement payload = root.TryGetProperty("payload", out JsonElement supplied) ? supplied : default;
        bool confirmed = root.TryGetProperty("commit", out JsonElement commit) && commit.ValueKind == JsonValueKind.True &&
            context.Metadata is not null && context.Metadata.TryGetValue("confirmedAction", out string? value) && value == "true";
        if (Mutations.Contains(operation) && !confirmed)
        {
            Guid actionId = Guid.CreateVersion7();
            return new(call.Id, JsonSerializer.Serialize(new { status = "prepared", actionId, arguments = root.GetRawText(), expiresAt = DateTimeOffset.UtcNow.AddMinutes(30) }));
        }

        object result = operation switch
        {
            "list_reminders" => await List(payload, cancellationToken),
            "get_reminder" => (await reminders.GetAsync(Id(payload), cancellationToken)).Value ?? throw new InvalidOperationException("Reminder was not found."),
            "list_reminders_by_source" => await reminders.GetBySourceAsync(Id(payload), cancellationToken),
            "get_reminder_delivery_state" => await GetDeliveryState(payload, cancellationToken),
            "create_reminder" => await Create(payload, cancellationToken),
            "update_reminder" => await Update(payload, cancellationToken),
            "delete_reminder" => await Delete(payload, cancellationToken),
            _ => throw new JsonException($"Unsupported reminder operation: {operation}.")
        };
        return new(call.Id, JsonSerializer.Serialize(result, JsonOptions));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        using JsonDocument document = JsonDocument.Parse(result.Output);
        JsonElement root = document.RootElement;
        return root.TryGetProperty("status", out JsonElement status) && status.ValueKind == JsonValueKind.String && status.GetString() == "prepared" &&
            root.TryGetProperty("actionId", out JsonElement actionId) && actionId.TryGetGuid(out Guid id) &&
            root.TryGetProperty("arguments", out JsonElement arguments) && arguments.ValueKind == JsonValueKind.String &&
            root.TryGetProperty("expiresAt", out JsonElement expiresAt) && expiresAt.TryGetDateTimeOffset(out DateTimeOffset expiry)
            ? new("Operations", "reminder_manage", id, arguments.GetString()!, expiry) : null;
    }

    private async Task<object> Create(JsonElement payload, CancellationToken ct)
    {
        ReminderWriteDto value = Read<ReminderWriteDto>(payload);
        Validate(value);
        return (await reminders.AddAsync(value, ct)).Value ?? throw new InvalidOperationException("Reminder could not be created.");
    }
    private async Task<object> Update(JsonElement payload, CancellationToken ct)
    {
        ReminderWriteDto value = Read<ReminderWriteDto>(payload);
        ReminderReadDto current = (await reminders.GetAsync(value.Id, ct)).Value ?? throw new InvalidOperationException("Reminder was not found.");
        if (current.IsSent) throw new InvalidOperationException("Sent reminders cannot be updated.");
        Validate(value);
        return (await reminders.UpdateAsync(value, ct)).Value ?? throw new InvalidOperationException("Reminder could not be updated.");
    }
    private async Task<object> Delete(JsonElement payload, CancellationToken ct)
    {
        ReminderReadDto reminder = (await reminders.GetAsync(Id(payload), ct)).Value ?? throw new InvalidOperationException("Reminder was not found.");
        if (reminder.IsSent) throw new InvalidOperationException("Sent reminders are audit records and cannot be deleted.");
        return await reminders.RemoveAsync(reminder.Id, ct);
    }
    private async Task<object> List(JsonElement payload, CancellationToken ct) => (await reminders.GetListAsync(Read<ReminderDescriptorDto>(payload), ct)).Value ?? [];
    private async Task<object> GetDeliveryState(JsonElement payload, CancellationToken ct)
    {
        ReminderReadDto reminder = (await reminders.GetAsync(Id(payload), ct)).Value ?? throw new InvalidOperationException("Reminder was not found.");
        if (reminder.Kind != ReminderKind.Sms || !long.TryParse(reminder.ProviderMessageId, out long messageId))
            return new { reminder, deliveryState = (object?)null, deliveryStateAvailable = false };
        return new { reminder, deliveryState = await messages.GetSmsDeliveryStateAsync(messageId, ct), deliveryStateAvailable = true };
    }
    private static T Read<T>(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? throw new JsonException("payload is required.") : JsonSerializer.Deserialize<T>(value.GetRawText(), JsonOptions) ?? throw new JsonException("payload is invalid.");
    private void Validate(ReminderWriteDto value)
    {
        if (string.IsNullOrWhiteSpace(value.SourceKind)) throw new JsonException("sourceKind is required.");
        if (!sourceRegistry.IsRegistered(value.SourceKind)) throw new JsonException($"sourceKind '{value.SourceKind}' is not registered for agent reminders.");
        if (value.TemplatePurpose is not ("AppointmentReminder" or "SeaReminder")) throw new JsonException("templatePurpose must name an approved Platform notification template.");
        if (!string.IsNullOrWhiteSpace(value.TemplateVariablesJson))
        {
            using JsonDocument variables = JsonDocument.Parse(value.TemplateVariablesJson);
            if (variables.RootElement.ValueKind != JsonValueKind.Object || variables.RootElement.EnumerateObject().Count() > 20)
                throw new JsonException("templateVariablesJson must be an object with at most 20 values.");
            foreach (JsonProperty property in variables.RootElement.EnumerateObject())
                if (property.Name.Length > 64 || property.Value.ValueKind != JsonValueKind.String || property.Value.GetString()!.Length > 256)
                    throw new JsonException("Template variable names or values exceed the allowed limits.");
        }
        if (!string.IsNullOrWhiteSpace(value.Recipient) && !Regex.IsMatch(value.Recipient, value.Kind == ReminderKind.Email ? @"^[^@\s]+@[^@\s]+\.[^@\s]+$" : @"^\+?[1-9]\d{7,14}$")) throw new JsonException("recipient has an invalid email or phone format.");
    }
    private static Guid Id(JsonElement payload) => (payload.TryGetProperty("id", out JsonElement value) || payload.TryGetProperty("sourceId", out value)) && value.TryGetGuid(out Guid id) ? id : throw new JsonException("id or sourceId must be a valid GUID.");
    private static string RequiredString(JsonElement payload, string name) => payload.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new JsonException($"{name} is required.");
}
