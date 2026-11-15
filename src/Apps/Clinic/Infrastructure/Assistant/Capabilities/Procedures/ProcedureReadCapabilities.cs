using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;
using Names = Vitarish.Clinic.Application.Assistant.Presentation.ClinicPresentableToolNames;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;

/// <summary>Patient page: read the current patient's procedures, steps, notes and files.</summary>
public sealed class ProcedureReadCapability(PatientProcedureRecords records)
    : AssistantCapability(ProcedureReadTools.Create(records, ProcedureToolWorkspace.Patient))
{
    public override string Name => ProcedureCapabilityNames.Reads;
    public override string Description => "Read the current patient's procedures, their ordered steps, and each step's notes, files and reminders.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read" };
    public override string GetInstructions() =>
        "For where a patient stands, read procedure progress first; it already includes each procedure's current and next step. " +
        "Read one step for its notes, files and reminder count. Lists are bounded, and a restricted, partial or unavailable record is not an absent one.";
}

/// <summary>Practice workspace: the same reads for a patient found by patient search in this turn.</summary>
public sealed class OperationsProcedureReadCapability(PatientProcedureRecords records)
    : AssistantCapability(ProcedureReadTools.Create(records, ProcedureToolWorkspace.Operations))
{
    public override string Name => ProcedureCapabilityNames.OperationsReads;
    public override string Description => "Read a searched patient's procedures, their ordered steps, and each step's notes, files and reminders.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == "practice" && AssistantOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() =>
        "Search for the patient first and pass the candidate token from this turn's search; tokens do not carry over to later turns. " +
        "For where a patient stands, read procedure progress first; it already includes each procedure's current and next step.";
}

internal static class ProcedureReadTools
{
    public static IReadOnlyCollection<IAgentTool> Create(PatientProcedureRecords records, ProcedureToolWorkspace workspace) =>
    [
        new ListProceduresTool(records, workspace),
        new ListStepsTool(records, workspace),
        new GetStepTool(records, workspace),
        new ListFilesTool(records, workspace)
    ];

    /// <summary>The tool's parameters: the operations workspace adds the patient's candidate token to each.</summary>
    public static string Schema(ProcedureToolWorkspace workspace, string properties, params string[] required)
    {
        bool operations = workspace == ProcedureToolWorkspace.Operations;
        string token = operations ? $"\"{PatientProcedureRecords.CandidateTokenArgument}\":{{\"type\":\"string\"}}" : string.Empty;
        string all = string.Join(",", new[] { token, properties }.Where(item => item.Length > 0));
        string[] names = operations ? [PatientProcedureRecords.CandidateTokenArgument, .. required] : required;
        return $"{{\"type\":\"object\",\"properties\":{{{all}}},\"required\":[{string.Join(",", names.Select(name => $"\"{name}\""))}],\"additionalProperties\":false}}";
    }

    public static int Limit(JsonElement arguments) =>
        arguments.TryGetProperty("limit", out JsonElement value) && value.TryGetInt32(out int limit) ? Math.Clamp(limit, 1, 20) : 10;

    public static Guid Id(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement value) && Guid.TryParse(value.GetString(), out Guid id)
            ? id
            : throw new JsonException($"{name} must be the id of a record read in this conversation.");

    public static AgentToolResult Result<T>(AgentToolCall call, T value) =>
        new(call.Id, JsonSerializer.Serialize(value, (JsonTypeInfo<T>)ProcedureReadJsonSerializerContext.Default.GetTypeInfo(typeof(T))!));
}

internal abstract class ProcedureReadTool(PatientProcedureRecords records, ProcedureToolWorkspace workspace) : IAgentTool
{
    protected PatientProcedureRecords Records { get; } = records;
    protected ProcedureToolWorkspace Workspace { get; } = workspace;
    protected bool IsOperations => Workspace == ProcedureToolWorkspace.Operations;
    public abstract AgentToolDefinition Definition { get; }

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        Guid patientId = await Records.ResolvePatientAsync(Workspace, document.RootElement, context, cancellationToken);
        return await ExecuteAsync(call, document.RootElement, patientId, context, cancellationToken);
    }

    protected abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct);
}

internal sealed class ListProceduresTool(PatientProcedureRecords records, ProcedureToolWorkspace workspace) : ProcedureReadTool(records, workspace)
{
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.ListOperationsPatientProcedures : Names.ListPatientProcedureProgress,
        "List up to 20 of the patient's procedures with progress: step counts, the current step (last committed) and the next open step.",
        ProcedureReadTools.Schema(workspace, "\"limit\":{\"type\":\"integer\"}"));

    protected override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct) =>
        ProcedureReadTools.Result(call, await Records.ListProceduresAsync(patientId, context, ProcedureReadTools.Limit(arguments), ct));
}

internal sealed class ListStepsTool(PatientProcedureRecords records, ProcedureToolWorkspace workspace) : ProcedureReadTool(records, workspace)
{
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.ListOperationsProcedureSteps : Names.ListPatientProcedureSteps,
        "List up to 20 steps of one of the patient's procedures in their order, with position, committed state and item count.",
        ProcedureReadTools.Schema(workspace, "\"procedureId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}", "procedureId"));

    protected override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct) =>
        ProcedureReadTools.Result(call, await Records.ListStepsAsync(patientId, ProcedureReadTools.Id(arguments, "procedureId"), context, ProcedureReadTools.Limit(arguments), ct));
}

internal sealed class GetStepTool(PatientProcedureRecords records, ProcedureToolWorkspace workspace) : ProcedureReadTool(records, workspace)
{
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.GetOperationsProcedureStep : Names.GetPatientProcedureStep,
        "Get one step of the patient's procedure: title, details, position, committed state, its latest notes (text), its files (metadata only) and how many reminders it has.",
        ProcedureReadTools.Schema(workspace, "\"stepId\":{\"type\":\"string\"}", "stepId"));

    protected override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct) =>
        ProcedureReadTools.Result(call, await Records.GetStepAsync(patientId, ProcedureReadTools.Id(arguments, "stepId"), context, ct));
}

internal sealed class ListFilesTool(PatientProcedureRecords records, ProcedureToolWorkspace workspace) : ProcedureReadTool(records, workspace)
{
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.ListOperationsStepFiles : Names.ListPatientStepFiles,
        "List up to 20 files of one step of the patient's procedure: name, type and size only, never their content.",
        ProcedureReadTools.Schema(workspace, "\"stepId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}", "stepId"));

    protected override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct) =>
        ProcedureReadTools.Result(call, await Records.ListFilesAsync(patientId, ProcedureReadTools.Id(arguments, "stepId"), context, ProcedureReadTools.Limit(arguments), ct));
}
