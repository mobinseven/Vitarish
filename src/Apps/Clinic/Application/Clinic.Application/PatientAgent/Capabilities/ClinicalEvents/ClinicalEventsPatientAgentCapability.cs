using System.Text.Json;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities.ClinicalEvents;

public sealed class ClinicalEventsPatientAgentCapability(
    GetPatientProcedureContextTool context,
    PrepareClinicalEventTool prepare) : PatientAgentCapability([context, prepare], ["add_clinical_event"])
{
    public override string Name => "ClinicalEvents";
    public override string Description => "Validate a patient procedure and prepare a clinician-authored clinical event for later review and confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read", "ClinicalEvents.Create" };
    public override string GetInstructions() =>
        "Use the current server-supplied patient, practice, and clinician scope. Validate the procedure before preparing. " +
        "A diagnosis is allowed only when the clinician explicitly asserted it; preserve its strength and meaning. " +
        "Ask for missing clinical information, show the complete proposal for review, and never confirm it during the model run.";
}

public abstract class ClinicalEventTool : IClinicalEventsPatientAgentTool
{
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);

    protected static ClinicalEventScope Scope(AgentExecutionContext context)
    {
        string Required(string key) => context.Metadata is not null &&
            context.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value : throw new InvalidOperationException($"The trusted clinical event scope is missing {key}.");
        IReadOnlySet<string> permissions = JsonSerializer.Deserialize<HashSet<string>>(Required("permissions"))
            ?? throw new InvalidOperationException("The trusted clinical event permissions are invalid.");
        return new(Guid.Parse(Required("patientId")), Required("practiceId"), Required("authenticatedUserId"),
            permissions, context.ExecutionId, Required("locale"), Required("timeZone"));
    }

    protected static AgentToolResult Result(AgentToolCall call, object? value) =>
        new(call.Id, JsonSerializer.Serialize(value));
}

public sealed class GetPatientProcedureContextTool(GetPatientProcedureContext useCase) : ClinicalEventTool
{
    public override AgentToolDefinition Definition { get; } = new(
        "get_patient_procedure_context",
        "Validate a procedure and return the next valid visit for the current patient.",
        "{\"type\":\"object\",\"properties\":{\"procedureId\":{\"type\":\"string\"}},\"required\":[\"procedureId\"],\"additionalProperties\":false}");

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument arguments = JsonDocument.Parse(call.ArgumentsJson);
        PatientProcedureContext? result = await useCase.ExecuteAsync(
            Scope(context), arguments.RootElement.GetProperty("procedureId").GetGuid(), cancellationToken);
        return Result(call, result);
    }
}

public sealed class PrepareClinicalEventTool(PrepareClinicalEvent useCase) : ClinicalEventTool, IPatientAgentPreparationTool
{
    private const string Schema = """
        {"type":"object","properties":{"procedureId":{"type":"string"},"visitNumber":{"type":"integer","minimum":1},"eventType":{"type":"string"},"completionStatus":{"type":"string"},"performedAt":{"type":"string"},"observations":{"type":"string"},"diagnosis":{"type":"string"},"diagnosisExplicitlyAssertedByClinician":{"type":"boolean"},"planOrFollowUp":{"type":"string"},"attachmentReferences":{"type":"array","items":{"type":"string"}},"narrativeNote":{"type":"string"}},"required":["procedureId","visitNumber","eventType","completionStatus","performedAt","diagnosisExplicitlyAssertedByClinician"],"additionalProperties":false}
        """;

    public override AgentToolDefinition Definition { get; } = new(
        "prepare_clinical_event", "Prepare a Clinic-owned clinical event proposal for explicit review; this does not confirm it.", Schema);

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement args = document.RootElement;
        string? Optional(string name) => args.TryGetProperty(name, out JsonElement value) ? value.GetString() : null;
        IReadOnlyList<string> attachments = args.TryGetProperty("attachmentReferences", out JsonElement refs)
            ? refs.EnumerateArray().Select(item => item.GetString()!).ToArray() : [];
        var content = new ClinicalEventContent(
            args.GetProperty("procedureId").GetGuid(), args.GetProperty("visitNumber").GetInt32(),
            args.GetProperty("eventType").GetString()!, args.GetProperty("completionStatus").GetString()!,
            args.GetProperty("performedAt").GetDateTimeOffset(), Optional("observations"), Optional("diagnosis"),
            args.GetProperty("diagnosisExplicitlyAssertedByClinician").GetBoolean(), Optional("planOrFollowUp"),
            attachments, Optional("narrativeNote"));
        return Result(call, await useCase.ExecuteAsync(new(Scope(context), content), cancellationToken));
    }

    public PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        ClinicalEventProposalResult? preparation = JsonSerializer.Deserialize<ClinicalEventProposalResult>(result.Output);
        return preparation is { Status: ClinicalEventProposalResultStatus.Prepared, Proposal: { } proposal }
            ? new("ClinicalEvents", proposal.ActionType, proposal.ActionId, JsonSerializer.Serialize(proposal), proposal.ExpiresAt)
            : null;
    }
}
