using System.Text.Json;
using MediatR;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Medical.Application.Queries.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Prescriptions;

public abstract class PatientPrescriptionReadTool(ISender sender) : IPrescriptionsPatientAgentTool
{
    protected ISender Sender { get; } = sender;

    protected static (Guid PatientId, Guid ClinicId) Scope(AgentExecutionContext context)
    {
        if (context.Metadata is null ||
            !context.Metadata.TryGetValue("patientId", out string? patient) || !Guid.TryParse(patient, out Guid patientId) ||
            !context.Metadata.TryGetValue("practiceId", out string? practice) || !Guid.TryParse(practice, out Guid clinicId))
            throw new InvalidOperationException("The trusted patient prescription scope is incomplete.");

        return (patientId, clinicId);
    }

    protected static int Limit(JsonElement arguments) =>
        arguments.TryGetProperty("limit", out JsonElement value) && value.TryGetInt32(out int limit)
            ? Math.Clamp(limit, 1, 20)
            : 10;

    private protected static AgentToolResult Result(AgentToolCall call, PatientPrescriptionHistoryResponse value) => new(call.Id, JsonSerializer.Serialize(value, PatientPrescriptionReadJsonSerializerContext.Default.PatientPrescriptionHistoryResponse));
    private protected static AgentToolResult Result(AgentToolCall call, PatientPrescriptionDetailResponse value) => new(call.Id, JsonSerializer.Serialize(value, PatientPrescriptionReadJsonSerializerContext.Default.PatientPrescriptionDetailResponse));
    private protected static AgentToolResult Result(AgentToolCall call, PatientPrescriptionAttentionResponse value) => new(call.Id, JsonSerializer.Serialize(value, PatientPrescriptionReadJsonSerializerContext.Default.PatientPrescriptionAttentionResponse));
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
}

public sealed class ListPatientPrescriptionHistoryTool(ISender sender) : PatientPrescriptionReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("list_patient_prescription_history", "List up to 20 current-patient prescription records in newest-first order.", "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\"}},\"additionalProperties\":false}");

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        int limit = Limit(document.RootElement);
        (Guid patientId, Guid clinicId) = Scope(context);
        IReadOnlyList<PrescriptionSummaryDto> values = await Sender.Send(new GetPrescriptionHistoryQuery
        {
            PatientId = patientId,
            ClinicId = clinicId,
            Take = limit + 1
        }, cancellationToken);

        return Result(call, new PatientPrescriptionHistoryResponse(values.Take(limit).Select(Summary).ToArray(), values.Count > limit));
    }

    internal static PatientPrescriptionSummary Summary(PrescriptionSummaryDto item) => new(item.Id, item.Status.ToString(), item.SelectedProviderKey, item.ItemCount, item.CreatedAt, item.SubmittedAt, item.TrackingCode, item.ItemDisplayNames, item.SubmittedAt ?? item.CreatedAt);
}

public sealed class GetPatientPrescriptionTool(ISender sender) : PatientPrescriptionReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_prescription", "Get one prescription only after verifying it belongs to the current patient and clinic.", "{\"type\":\"object\",\"properties\":{\"prescriptionId\":{\"type\":\"string\"}},\"required\":[\"prescriptionId\"],\"additionalProperties\":false}");

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        if (!Guid.TryParse(document.RootElement.GetProperty("prescriptionId").GetString(), out Guid prescriptionId))
            return Result(call, new PatientPrescriptionDetailResponse(false));

        (Guid patientId, Guid clinicId) = Scope(context);
        PrescriptionDto? item = await Sender.Send(new GetPrescriptionQuery { PrescriptionId = prescriptionId }, cancellationToken);
        if (item is null || item.PatientId != patientId || item.ClinicId != clinicId)
            return Result(call, new PatientPrescriptionDetailResponse(false));

        return Result(call, new PatientPrescriptionDetailResponse(true, new PatientPrescriptionDetail(item.Id, item.Status.ToString(), item.SelectedProviderKey, item.VersionNumber, item.CreatedAt, item.SubmittedAt, item.TrackingCode, item.Items.Take(20).Select(value => new PatientPrescriptionItem(value.Category, value.ReferenceCode, value.DisplayName, value.Quantity, value.DurationDays, value.SortOrder)).ToArray(), item.Items.Count > 20, item.SubmittedAt ?? item.CreatedAt)));
    }
}

public sealed class GetPatientPrescriptionAttentionTool(ISender sender) : PatientPrescriptionReadTool(sender)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_prescription_attention", "Get the current patient's prescription attention count and up to 20 non-terminal records.", "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\"}},\"additionalProperties\":false}");

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        int limit = Limit(document.RootElement);
        (Guid patientId, Guid clinicId) = Scope(context);
        PatientPrescriptionDashboardDto dashboard = await Sender.Send(new GetPatientPrescriptionDashboardQuery
        {
            PatientId = patientId,
            ClinicId = clinicId,
            AttentionTake = limit
        }, cancellationToken);

        return Result(call, new PatientPrescriptionAttentionResponse(dashboard.TotalCount, dashboard.AttentionCount, dashboard.AttentionQueue.Select(ListPatientPrescriptionHistoryTool.Summary).ToArray(), dashboard.AttentionCount > dashboard.AttentionQueue.Count));
    }
}
