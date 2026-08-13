using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Sea;

public sealed class SeaPatientReadAgentCapability(
    ListPatientProceduresTool procedures,
    ListPatientProcedureEventsTool events,
    GetPatientProcedureEventTool detail,
    ListPatientEventAttachmentMetadataTool attachments)
    : ClinicWorkspaceCapability([procedures, events, detail, attachments])
{
    public override string Name => "SeaPatientReads";
    public override string Description => "Read the current patient's SEA-owned procedure progress, events, and attachment metadata.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read" };
    public override string GetInstructions() =>
        "Use procedure progress for routine status, then drill into events or attachment metadata only when asked. Lists are bounded and partial or unavailable data is not absence.";
}

public abstract class SeaPatientReadTool(IProceduresService procedures, IEventsService events) : IAgentTool
{
    protected IProceduresService Procedures { get; } = procedures;
    protected IEventsService Events { get; } = events;
    protected static Guid PatientId(AgentExecutionContext context) =>
        context.Metadata is not null && context.Metadata.TryGetValue("patientId", out string? value) && Guid.TryParse(value, out Guid patientId)
            ? patientId : throw new InvalidOperationException("The trusted SEA patient scope is incomplete.");
    protected async Task<ProcedureReadDto?> ProcedureAsync(Guid procedureId, AgentExecutionContext context, CancellationToken ct)
    {
        ProcedureReadDto? procedure = (await Procedures.GetAsync(procedureId, ct)).Value;
        return procedure?.ProjectId == PatientId(context) ? procedure : null;
    }
    protected async Task<EventReadDto?> EventAsync(Guid eventId, AgentExecutionContext context, CancellationToken ct)
    {
        EventReadDto? value = (await Events.GetAsync(eventId, ct)).Value;
        return value?.ProjectId == PatientId(context) ? value : null;
    }
    protected static int Limit(JsonElement arguments) => arguments.TryGetProperty("limit", out JsonElement value) && value.TryGetInt32(out int limit) ? Math.Clamp(limit, 1, 20) : 10;
    private protected static AgentToolResult Result(AgentToolCall call, SeaProcedureListResponse value) => new(call.Id, JsonSerializer.Serialize(value, SeaPatientReadJsonSerializerContext.Default.SeaProcedureListResponse));
    private protected static AgentToolResult Result(AgentToolCall call, SeaEventListResponse value) => new(call.Id, JsonSerializer.Serialize(value, SeaPatientReadJsonSerializerContext.Default.SeaEventListResponse));
    private protected static AgentToolResult Result(AgentToolCall call, SeaEventDetailResponse value) => new(call.Id, JsonSerializer.Serialize(value, SeaPatientReadJsonSerializerContext.Default.SeaEventDetailResponse));
    private protected static AgentToolResult Result(AgentToolCall call, SeaAttachmentListResponse value) => new(call.Id, JsonSerializer.Serialize(value, SeaPatientReadJsonSerializerContext.Default.SeaAttachmentListResponse));
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
}

public sealed class ListPatientProceduresTool(IProceduresService procedures, IEventsService events) : SeaPatientReadTool(procedures, events)
{
    public override AgentToolDefinition Definition { get; } = new("list_patient_procedure_progress", "List up to 20 current-patient procedures and their recorded progress.", "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\"}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        int limit = Limit(document.RootElement);
        IReadOnlyList<ProcedureReadDto> values = (await Procedures.GetListAsync(new ProcedureDescriptorDto { ProjectId = PatientId(context) }, cancellationToken)).Value ?? [];
        ProcedureReadDto[] selected = values.Take(limit + 1).ToArray();
        return Result(call, new SeaProcedureListResponse(selected.Take(limit).Select(item => new SeaProcedureRead(item.Id, item.Name, item.Description, item.Stats, item.UpdatedAt ?? item.CreatedAt)).ToArray(), selected.Length > limit));
    }
}

public sealed class ListPatientProcedureEventsTool(IProceduresService procedures, IEventsService events) : SeaPatientReadTool(procedures, events)
{
    public override AgentToolDefinition Definition { get; } = new("list_patient_procedure_events", "List up to 20 recorded events for one current-patient procedure.", "{\"type\":\"object\",\"properties\":{\"procedureId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}},\"required\":[\"procedureId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        if (!Guid.TryParse(document.RootElement.GetProperty("procedureId").GetString(), out Guid procedureId) || await ProcedureAsync(procedureId, context, cancellationToken) is null)
            return Result(call, new SeaEventListResponse(false));
        int limit = Limit(document.RootElement);
        EventReadDto[] values = (await Events.GetByProcedureAsync(procedureId, cancellationToken)).OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt).Take(limit + 1).ToArray();
        return Result(call, new SeaEventListResponse(true, values.Take(limit).Select(item => new SeaEventRead(item.Id, item.Title, item.IsCommitted, item.CommittedAt, item.UpdatedAt ?? item.CreatedAt)).ToArray(), values.Length > limit));
    }
}

public sealed class GetPatientProcedureEventTool(IProceduresService procedures, IEventsService events) : SeaPatientReadTool(procedures, events)
{
    public override AgentToolDefinition Definition { get; } = new("get_patient_procedure_event", "Get detail for one current-patient procedure event.", "{\"type\":\"object\",\"properties\":{\"eventId\":{\"type\":\"string\"}},\"required\":[\"eventId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        if (!Guid.TryParse(document.RootElement.GetProperty("eventId").GetString(), out Guid eventId) || await EventAsync(eventId, context, cancellationToken) is not { } item)
            return Result(call, new SeaEventDetailResponse(false));
        return Result(call, new SeaEventDetailResponse(true, new SeaEventDetail(item.Id, item.Title, item.Description, item.IsCommitted, item.CommittedAt, item.UpdatedAt ?? item.CreatedAt)));
    }
}

public sealed class ListPatientEventAttachmentMetadataTool(IProceduresService procedures, IEventsService events, IAttachmentsService attachments) : SeaPatientReadTool(procedures, events)
{
    public override AgentToolDefinition Definition { get; } = new("list_patient_event_attachment_metadata", "List metadata only, never binary content, for one current-patient event's attachments.", "{\"type\":\"object\",\"properties\":{\"eventId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}},\"required\":[\"eventId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        if (!Guid.TryParse(document.RootElement.GetProperty("eventId").GetString(), out Guid eventId) || await EventAsync(eventId, context, cancellationToken) is null)
            return Result(call, new SeaAttachmentListResponse(false));
        int limit = Limit(document.RootElement);
        AttachmentReadDto[] values = (await attachments.GetByEventAsync(eventId, cancellationToken)).Take(limit + 1).ToArray();
        return Result(call, new SeaAttachmentListResponse(true, values.Take(limit).Select(item => new SeaAttachmentRead(item.Id, item.FileName, item.ContentType, item.Size, item.Kind.ToString(), item.UpdatedAt ?? item.CreatedAt)).ToArray(), values.Length > limit));
    }
}
