using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Domain.Models;
using MediatR;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Patients;

public sealed class ClinicPatientReadAgentCapability(
    GetPatientFactualOverviewTool overview,
    GetPatientProfileTool profile,
    GetPatientRecordedClinicalFactsTool clinicalFacts)
    : AssistantCapability([overview, profile, clinicalFacts])
{
    public override string Name => "ClinicPatientReads";
    public override string Description => "Read the current patient's Clinic-owned profile and recorded clinical facts.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read" };
    public override string GetInstructions() =>
        "Use the compact factual overview for broad questions, then use profile or recorded clinical facts only when specifically asked. Do not infer a diagnosis or summarize unrelated fields.";
}
public sealed class GetPatientFactualOverviewTool(ISender sender) : ClinicPatientReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_factual_overview", "Get a compact Clinic-owned factual overview for a broad patient question; use drill-down tools for detail.", "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        PatientView? patient = await PatientAsync(context, cancellationToken);
        return Result(call, patient is null ? new ClinicPatientOverviewResponse(false) : new ClinicPatientOverviewResponse(true, SourceTime(patient), new(patient.Name, patient.DateOfBirth, patient.VisitReason, !string.IsNullOrWhiteSpace(patient.Allergies), !string.IsNullOrWhiteSpace(patient.CurrentMedications))));
    }
}
public abstract class ClinicPatientReadTool(ISender sender) : IClinicalEventsAssistantTool
{
    protected async Task<PatientView?> PatientAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.Metadata is null || !context.Metadata.TryGetValue("patientId", out string? value) || !Guid.TryParse(value, out Guid patientId))
            throw new InvalidOperationException("The trusted Clinic patient scope is incomplete.");
        return await sender.Send(new GetPatientByIdQuery(patientId), cancellationToken);
    }

    protected DateTimeOffset SourceTime(PatientView patient) => new(patient.UpdatedAt ?? patient.CreatedAt, TimeSpan.Zero);
    private protected static AgentToolResult Result(AgentToolCall call, ClinicPatientOverviewResponse value) => new(call.Id, JsonSerializer.Serialize(value, ClinicPatientReadJsonSerializerContext.Default.ClinicPatientOverviewResponse));
    private protected static AgentToolResult Result(AgentToolCall call, ClinicPatientProfileResponse value) => new(call.Id, JsonSerializer.Serialize(value, ClinicPatientReadJsonSerializerContext.Default.ClinicPatientProfileResponse));
    private protected static AgentToolResult Result(AgentToolCall call, ClinicPatientFactsResponse value) => new(call.Id, JsonSerializer.Serialize(value, ClinicPatientReadJsonSerializerContext.Default.ClinicPatientFactsResponse));
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
}

public sealed class GetPatientProfileTool(ISender sender) : ClinicPatientReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_profile", "Get the current patient's recorded Clinic profile and contact facts.", "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        PatientView? patient = await PatientAsync(context, cancellationToken);
        return Result(call, patient is null ? new ClinicPatientProfileResponse(false) : new ClinicPatientProfileResponse(true, SourceTime(patient), new(patient.Name, patient.FirstName, patient.LastName, patient.DateOfBirth, patient.Gender.ToString(), patient.Phone, patient.Email, patient.Address, patient.InsuranceProvider)));
    }
}

public sealed class GetPatientRecordedClinicalFactsTool(ISender sender) : ClinicPatientReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_recorded_clinical_facts", "Get only the current patient's recorded Clinic facts; never infer clinical conclusions.", "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        PatientView? patient = await PatientAsync(context, cancellationToken);
        return Result(call, patient is null ? new ClinicPatientFactsResponse(false) : new ClinicPatientFactsResponse(true, SourceTime(patient), new(patient.VisitReason, patient.Allergies, patient.CurrentMedications, patient.ClinicalNotes)));
    }
}
