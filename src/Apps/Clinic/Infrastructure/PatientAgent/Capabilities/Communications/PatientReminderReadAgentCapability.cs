using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Communications;

public sealed class PatientReminderReadAgentCapability(ListPatientReminderStatusTool reminders)
    : PatientAgentCapability([reminders])
{
    public override string Name => "PatientReminderReads";
    public override string Description => "Read source-validated reminder delivery status for the current patient.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "Communications.Read" };
    public override string GetInstructions() =>
        "Use reminder status only when it answers the question. A missing or unavailable reminder is not evidence that no reminder was planned.";
}

public sealed class ListPatientReminderStatusTool(IRemindersService reminders, IReservationManager reservations, IEventsService events) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new("list_patient_reminder_status", "List up to 20 reminders for one appointment or SEA event after verifying that source belongs to the current patient.", "{\"type\":\"object\",\"properties\":{\"sourceType\":{\"type\":\"string\",\"enum\":[\"appointment\",\"event\"]},\"sourceId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}},\"required\":[\"sourceType\",\"sourceId\"],\"additionalProperties\":false}");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.Metadata is null || !context.Metadata.TryGetValue("patientId", out string? rawPatientId) || !Guid.TryParse(rawPatientId, out Guid patientId))
            throw new InvalidOperationException("The trusted patient reminder scope is incomplete.");

        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        if (!Guid.TryParse(document.RootElement.GetProperty("sourceId").GetString(), out Guid sourceId))
            return Result(call, new PatientReminderStatusResponse(false));
        string sourceType = document.RootElement.GetProperty("sourceType").GetString() ?? string.Empty;
        bool ownsSource = sourceType switch
        {
            "appointment" => (await reservations.GetAsync(sourceId, cancellationToken)).Value?.ReservingPartyId == patientId.ToString("D"),
            "event" => (await events.GetAsync(sourceId, cancellationToken)).Value?.ProjectId == patientId,
            _ => false
        };
        if (!ownsSource) return Result(call, new PatientReminderStatusResponse(false));
        int limit = document.RootElement.TryGetProperty("limit", out JsonElement value) && value.TryGetInt32(out int parsed)
            ? Math.Clamp(parsed, 1, 20)
            : 10;
        IReadOnlyList<ReminderReadDto> values = await reminders.GetBySourceAsync(sourceId, cancellationToken);
        ReminderReadDto[] selected = values.OrderByDescending(item => item.RemindAt).Take(limit + 1).ToArray();
        return Result(call, new PatientReminderStatusResponse(true, selected.Take(limit).Select(item => new PatientReminderStatus(item.Id, item.Kind.ToString(), item.SourceKind, item.TemplatePurpose, item.RemindAt, item.SentAt, item.IsSent, item.ProviderSubmittedAt, item.SentAt ?? item.ProviderSubmittedAt ?? item.CreatedAt)).ToArray(), selected.Length > limit));
    }

    private static AgentToolResult Result(AgentToolCall call, PatientReminderStatusResponse value) =>
        new(call.Id, JsonSerializer.Serialize(value, PatientReminderReadJsonSerializerContext.Default.PatientReminderStatusResponse));
}
