using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.AgentCapabilities.ClinicalEvents;
using Vitarish.Sea.Interface.AgentCapabilities;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities.ClinicalEvents;

public sealed class ClinicalEventAgentCapability(
    GetProcedureContextTool context,
    PrepareClinicalEventTool prepare,
    ConfirmClinicalEventTool confirm) : PatientAgentCapability([context, prepare, confirm])
{
    public override string Name => "ClinicalEvents";
    public override string Description => "Resolve a Sea procedure and prepare a structured clinical event for review before confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read", "ClinicalEvents.Create" };
    public override string GetInstructions() =>
        "Resolve the procedure and validate the visit/session first. Preserve clinician assertions in their specific fields. " +
        "Never diagnose or add diagnosis text: diagnosis must be explicitly stated by the clinician. Ask for all missing material fields. " +
        "Show both structuredContent and narrativeNote from the proposal, then call confirm_clinical_event only after explicit confirmation.";
}

public abstract class ClinicalEventTool(IClinicalEventAgentCapability capability) : IClinicalEventsPatientAgentTool
{
    protected IClinicalEventAgentCapability Capability { get; } = capability;
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);

    protected static string Metadata(AgentExecutionContext context, string name) =>
        context.Metadata is not null && context.Metadata.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidOperationException($"The trusted clinical-event scope is missing {name}.");

    protected static ClinicalEventScope Scope(AgentExecutionContext context) => new(
        Guid.Parse(Metadata(context, "patientId")), Metadata(context, "practiceId"), Metadata(context, "authenticatedUserId"));

    protected static AgentToolResult Result(AgentToolCall call, object? value) => new(call.Id, JsonSerializer.Serialize(value));
    protected static string? String(JsonElement args, string name) => args.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
}

public sealed class GetProcedureContextTool(IClinicalEventAgentCapability capability) : ClinicalEventTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_procedure_context",
        "Resolve a Sea-owned procedure for the current patient and return its valid visit/session numbers.",
        "{\"type\":\"object\",\"properties\":{\"procedureId\":{\"type\":\"string\"}},\"required\":[\"procedureId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        return Result(call, await Capability.GetPatientProcedureContextAsync(Scope(context), args.RootElement.GetProperty("procedureId").GetGuid(), cancellationToken));
    }
}

public sealed class PrepareClinicalEventTool(IClinicalEventAgentCapability capability) : ClinicalEventTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("prepare_clinical_event",
        "Prepare structured and narrative Sea clinical-event content without committing it.",
        """
        {"type":"object","properties":{"procedureId":{"type":"string"},"visitOrSessionNumber":{"type":"integer"},"eventType":{"type":"string"},"completionStatus":{"type":"string"},"performedAt":{"type":"string"},"clinicianAuthoredObservations":{"type":"string"},"clinicianAuthoredDiagnosisText":{"type":"string"},"planOrFollowUp":{"type":"string"},"attachmentReferences":{"type":"array","items":{"type":"string"}},"generatedDiagnosisText":{"type":"string"}},"required":["procedureId","visitOrSessionNumber","eventType","completionStatus","performedAt"],"additionalProperties":false}
        """);

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement root = args.RootElement;
        ClinicalEventScope scope = Scope(context);
        IReadOnlyList<string>? attachments = root.TryGetProperty("attachmentReferences", out JsonElement values)
            ? values.EnumerateArray().Select(value => value.GetString()!).ToArray() : null;
        ClinicalEventProvenance provenance = new("AgentAssisted", context.ExecutionId, scope.UserId, null);
        PrepareClinicalEventRequest request = new(scope, root.GetProperty("procedureId").GetGuid(),
            root.GetProperty("visitOrSessionNumber").GetInt32(), String(root, "eventType"), String(root, "completionStatus"),
            root.GetProperty("performedAt").GetDateTimeOffset(), String(root, "clinicianAuthoredObservations"),
            String(root, "clinicianAuthoredDiagnosisText"), String(root, "planOrFollowUp"), attachments,
            String(root, "generatedDiagnosisText"), provenance);
        return Result(call, await Capability.PrepareClinicalEventAsync(request, cancellationToken));
    }
}

public sealed class ConfirmClinicalEventTool(IClinicalEventAgentCapability capability) : ClinicalEventTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("confirm_clinical_event",
        "Commit a reviewed immutable clinical-event proposal after explicit clinician confirmation.",
        "{\"type\":\"object\",\"properties\":{\"proposalId\":{\"type\":\"string\"}},\"required\":[\"proposalId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        ClinicalEventScope scope = Scope(context);
        return Result(call, await Capability.ConfirmClinicalEventProposalAsync(args.RootElement.GetProperty("proposalId").GetGuid(), scope, scope.UserId, cancellationToken));
    }
}
