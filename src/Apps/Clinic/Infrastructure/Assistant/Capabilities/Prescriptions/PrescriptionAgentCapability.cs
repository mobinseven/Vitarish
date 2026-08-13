using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Prescriptions;

public sealed class PrescriptionAgentCapability(
    PreparePrescriptionTool prepare) : ClinicWorkspaceCapability([prepare], [
        ClinicWorkspaceActionPolicies.Prepare("create_prescription", "prescriptions", ["Prescriptions.Create"], toolNames: ["prepare_prescription"])
    ])
{
    public override string Name => "Prescriptions";
    public override string Description => "Prepare a Medical-owned prescription draft from a clinician-selected template and insurance coverage for later explicit confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read", "Prescriptions.Create" };
    public override string GetInstructions() =>
        "Never diagnose or choose/infer medication, dosage, frequency, route, duration, instructions, coverage, contraindications, or eligibility. " +
        "Ask the clinician for every missing material field. Coverage must be explicitly named. Prepare and show the entire returned draft; do not submit it.";
}

public sealed class PrescriptionReadAgentCapability(
    SearchPrescriptionTemplatesTool search,
    GetPrescriptionTemplateRequirementsTool requirements,
    ListPatientPrescriptionHistoryTool history,
    GetPatientPrescriptionTool prescription,
    GetPatientPrescriptionAttentionTool attention) : ClinicWorkspaceCapability([search, requirements, history, prescription, attention])
{
    public override string Name => "PrescriptionReads";
    public override string Description => "Read authoritative prescription template details without creating a prescription.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read" };
    public override string GetInstructions() =>
        "Use only for factual template and requirement questions; do not infer clinical treatment.";
}

public abstract class PrescriptionTool(IPrescriptionAgentCapability capability) : IPrescriptionsClinicWorkspaceTool
{
    protected IPrescriptionAgentCapability Capability { get; } = capability;
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
    protected static AgentToolResult Result(AgentToolCall call, object? value) => new(call.Id, JsonSerializer.Serialize(value));
    protected static string RequiredMetadata(AgentExecutionContext context, string key) =>
        context.Metadata is not null && context.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidOperationException($"The trusted prescription scope is missing {key}.");
    protected static PrescriptionAgentScope Scope(AgentExecutionContext context)
    {
        Guid patientId = Guid.Parse(RequiredMetadata(context, "patientId"));
        string practiceId = RequiredMetadata(context, "practiceId");
        Guid clinicId = Guid.TryParse(practiceId, out Guid parsedClinic) ? parsedClinic : throw new InvalidOperationException("The trusted practice id is not a clinic identifier.");
        string userId = RequiredMetadata(context, "authenticatedUserId");
        Guid doctorId = Guid.TryParse(userId, out Guid parsedDoctor) ? parsedDoctor : throw new InvalidOperationException("The authenticated clinician id is invalid.");
        string? patientNationalId = context.Metadata is not null && context.Metadata.TryGetValue("patientNationalId", out string? nationalId) ? nationalId : null;
        return new(practiceId, patientId, patientNationalId, doctorId, clinicId, userId);
    }
}

public sealed class SearchPrescriptionTemplatesTool(IPrescriptionAgentCapability capability) : PrescriptionTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("search_prescription_templates", "Resolve the clinician-requested active prescription template without choosing one on the clinician's behalf.", "{\"type\":\"object\",\"properties\":{\"templateName\":{\"type\":\"string\"}},\"required\":[\"templateName\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson); return Result(call, await Capability.SearchAsync(RequiredMetadata(context, "practiceId"), args.RootElement.GetProperty("templateName").GetString()!, cancellationToken)); }
}

public sealed class GetPrescriptionTemplateRequirementsTool(IPrescriptionAgentCapability capability) : PrescriptionTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("get_prescription_template_requirements", "Return authoritative template items and identify missing material prescription fields.", "{\"type\":\"object\",\"properties\":{\"templateId\":{\"type\":\"string\"}},\"required\":[\"templateId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson); return Result(call, await Capability.GetRequirementsAsync(args.RootElement.GetProperty("templateId").GetGuid(), RequiredMetadata(context, "practiceId"), cancellationToken)); }
}

public sealed class PreparePrescriptionTool(IPrescriptionAgentCapability capability) : PrescriptionTool(capability), IClinicWorkspacePreparationTool
{
    public override AgentToolDefinition Definition { get; } = new("prepare_prescription", "Create a Medical-owned draft for complete review; this does not submit it.", "{\"type\":\"object\",\"properties\":{\"templateId\":{\"type\":\"string\"},\"templateVersion\":{\"type\":\"string\"},\"insuranceCoverage\":{\"type\":\"string\"}},\"required\":[\"templateId\",\"templateVersion\",\"insuranceCoverage\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson); PrescriptionAgentScope scope = Scope(context);
        var provenance = new PrescriptionAgentProvenance("AgentAssisted", context.ExecutionId, call.Id, scope.RequestedByUserId, null, args.RootElement.GetProperty("templateVersion").GetString()!);
        return Result(call, await Capability.PrepareAsync(args.RootElement.GetProperty("templateId").GetGuid(), args.RootElement.GetProperty("insuranceCoverage").GetString(), scope, provenance, cancellationToken));
    }

    public ClinicWorkspacePreparedAction? GetPreparedAction(AgentToolResult result)
    {
        PrescriptionDraftPreparationResult? preparation = JsonSerializer.Deserialize<PrescriptionDraftPreparationResult>(result.Output);
        return preparation is { Status: PrescriptionDraftPreparationStatus.Prepared, Review: { } review }
            ? new("Prescriptions", "create_prescription", review.ReviewId, JsonSerializer.Serialize(review), null)
            : null;
    }
}

public sealed class ConfirmPrescriptionTool(IPrescriptionAgentCapability capability) : PrescriptionTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("confirm_prescription", "Submit a fully reviewed Medical prescription draft after explicit clinician confirmation.", "{\"type\":\"object\",\"properties\":{\"reviewId\":{\"type\":\"string\"}},\"required\":[\"reviewId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson); PrescriptionAgentScope scope = Scope(context); return Result(call, await Capability.ConfirmAsync(args.RootElement.GetProperty("reviewId").GetGuid(), scope, scope.RequestedByUserId, cancellationToken)); }
}
