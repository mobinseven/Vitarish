using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;

public sealed class OperationsAgentCapability(IEnumerable<IClinicOperationsAgentTool> tools)
    : PatientAgentCapability([.. tools], [
        "book_appointment", "reschedule_appointment", "cancel_appointment", "reject_appointment",
        "create_shift", "edit_shift", "close_shift", "cancel_shift", "archive_shift", "appointment_manage", "reminder_manage"])
{
    public override string Name => "Operations";
    public override string Description => "Search patients and inspect or safely operate the authenticated practice schedule.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(PatientAgentContext context) =>
        context.SubjectKind == "practice" &&
        PatientAgentOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() =>
        "Use patient search before patient appointment operations. Direct transitions are limited to confirm, present, complete, activate, and pause. " +
        "All other schedule mutations create immutable proposals for explicit review; never claim a proposal was executed during the run.";
}

public interface IClinicOperationsAgentTool : IAgentTool;

public abstract class OperationsTool(IAppointmentSchedulingAgentCapability scheduling) : IClinicOperationsAgentTool
{
    protected IAppointmentSchedulingAgentCapability Scheduling { get; } = scheduling;
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
    protected static AgentSchedulingContext Scope(AgentToolCall call, AgentExecutionContext context) =>
        new(Required(context, "practiceId"), context.ExecutionId, call.Id, call.Name, Required(context, "authenticatedUserId"));
    protected static string Required(AgentExecutionContext context, string key) =>
        context.Metadata is not null && context.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidOperationException($"The operations scope is missing {key}.");
    protected static AgentToolResult Result<T>(AgentToolCall call, T value)
    {
        JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>?)OperationsAgentToolJsonSerializerContext.Default.GetTypeInfo(typeof(T))
            ?? throw new InvalidOperationException($"No source-generated JSON metadata is registered for {typeof(T).Name}.");
        return new(call.Id, JsonSerializer.Serialize(value, typeInfo));
    }

    protected static AgentToolResult ResultWithoutPatientIdentity(
        AgentToolCall call,
        AgentSchedulingResult<IReadOnlyList<Vitarish.Appointments.Domain.Dto.ReservationReadDto>> result) =>
        Result(call, new OperationsAgentToolResponse<IReadOnlyList<OperationsAppointmentRead>>(
            result.Status.ToString(),
            result.Value?.Select(OperationsAppointmentRead.From).ToArray(),
            result.OperationId,
            result.Message));

    protected static AgentToolResult ResultWithoutPatientIdentity(
        AgentToolCall call,
        AgentSchedulingResult<Vitarish.Appointments.Domain.Dto.ReservationReadDto> result) =>
        Result(call, new OperationsAgentToolResponse<OperationsAppointmentRead>(
            result.Status.ToString(),
            result.Value is null ? null : OperationsAppointmentRead.From(result.Value),
            result.OperationId,
            result.Message));
    protected static Guid Guid(JsonElement args, string name) => args.GetProperty(name).GetGuid();
    protected static DateOnly Date(JsonElement args, string name) => DateOnly.Parse(args.GetProperty(name).GetString()!);
    protected static DateOnly? OptionalDate(JsonElement args, string name) => args.TryGetProperty(name, out JsonElement value) ? DateOnly.Parse(value.GetString()!) : null;
    protected static int? OptionalInt(JsonElement args, string name) => args.TryGetProperty(name, out JsonElement value) ? value.GetInt32() : null;
    protected static string? OptionalString(JsonElement args, string name) => args.TryGetProperty(name, out JsonElement value) ? value.GetString() : null;
    protected static string Candidate(JsonElement args) => args.GetProperty("candidateToken").GetString() ?? throw new JsonException("candidateToken is required.");
    protected static AgentShiftInput Shift(JsonElement args, bool requireId) => new(
        Enum.Parse<Vitarish.Appointments.Domain.Enums.ShiftVisibility>(args.GetProperty("visibility").GetString()!, true),
        Enum.Parse<DayOfWeek>(args.GetProperty("dayOfWeek").GetString()!, true),
        TimeOnly.Parse(args.GetProperty("startTime").GetString()!), TimeOnly.Parse(args.GetProperty("endTime").GetString()!),
        args.GetProperty("timeZone").GetString()!, args.GetProperty("totalSeats").GetInt32(),
        args.TryGetProperty("serviceProviderId", out JsonElement provider) ? provider.GetGuid() : null,
        args.TryGetProperty("serviceId", out JsonElement service) ? service.GetGuid() : null,
        OptionalInt(args, "bookingOpensBefore"), OptionalInt(args, "bookingCloseBefore"),
        requireId ? Guid(args, "shiftId") : default);
}

public sealed class SearchPatientsTool(IPatientAgentPatientSearch search, IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("search_patients", "Search up to five patients in the authenticated practice. Returns opaque candidate tokens.", "{\"type\":\"object\",\"properties\":{\"search\":{\"type\":\"string\",\"maxLength\":100}},\"required\":[\"search\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson);
        string query = doc.RootElement.GetProperty("search").GetString() ?? string.Empty;
        if (query.Length > 100) throw new JsonException("Search is too long.");
        return Result(call, await search.SearchAsync(Required(context, "practiceId"), Required(context, "authenticatedUserId"), context.ExecutionId, query, cancellationToken));
    }
}

public sealed class ListOperationsAppointmentsTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("list_operations_appointments", "List appointments in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"dateFrom\":{\"type\":\"string\"},\"dateTo\":{\"type\":\"string\"},\"take\":{\"type\":\"integer\"}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a = doc.RootElement; AgentSchedulingContext s = Scope(call, context); return ResultWithoutPatientIdentity(call, await Scheduling.ListAppointmentsAsync(s, new(DateFrom: OptionalDate(a,"dateFrom"), DateTo: OptionalDate(a,"dateTo"), Take: a.TryGetProperty("take", out JsonElement take) ? take.GetInt32() : 50), cancellationToken)); }
}

public sealed class ListOperationsShiftsTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("list_operations_shifts", "List shifts in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"take\":{\"type\":\"integer\"}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a = doc.RootElement; return Result(call, await Scheduling.ListShiftsAsync(Scope(call,context), new(Take: a.TryGetProperty("take", out JsonElement take) ? take.GetInt32() : 50), cancellationToken)); }
}

public sealed class CountOperationsAppointmentsTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("count_operations_appointments", "Count appointments in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"dateFrom\":{\"type\":\"string\"},\"dateTo\":{\"type\":\"string\"}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a = doc.RootElement; return Result(call, await Scheduling.CountAppointmentsAsync(Scope(call,context), new(DateFrom: OptionalDate(a,"dateFrom"), DateTo: OptionalDate(a,"dateTo")), cancellationToken)); }
}

public sealed class GetOperationsAppointmentTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("get_operations_appointment", "Get one appointment in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); return ResultWithoutPatientIdentity(call, await Scheduling.GetAppointmentAsync(Scope(call,context), Guid(doc.RootElement,"id"), cancellationToken)); }
}

public sealed class GetOperationsAvailabilityTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("get_operations_availability", "Get bookable appointment slots in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"dateFrom\":{\"type\":\"string\"},\"dateTo\":{\"type\":\"string\"},\"shiftId\":{\"type\":\"string\"}},\"required\":[\"dateFrom\",\"dateTo\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a = doc.RootElement; return Result(call, await Scheduling.GetAvailabilityAsync(Scope(call,context), new(Date(a,"dateFrom"),Date(a,"dateTo"),a.TryGetProperty("shiftId",out JsonElement id)?id.GetGuid():null), cancellationToken)); }
}

public sealed class GetOperationsShiftTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("get_operations_shift", "Get one shift in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); return Result(call, await Scheduling.GetShiftAsync(Scope(call,context), Guid(doc.RootElement,"id"), cancellationToken)); }
}

public sealed class GetOperationsOccurrencesTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("get_operations_shift_occurrences", "Get shift occurrences in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"shiftId\":{\"type\":\"string\"},\"dateFrom\":{\"type\":\"string\"},\"dateTo\":{\"type\":\"string\"}},\"required\":[\"shiftId\",\"dateFrom\",\"dateTo\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a=doc.RootElement; return Result(call, await Scheduling.GetShiftOccurrencesAsync(Scope(call,context), new(Guid(a,"shiftId"),Date(a,"dateFrom"),Date(a,"dateTo")), cancellationToken)); }
}

public sealed class ResolveOperationsServicesTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("resolve_operations_services", "Resolve services in the authenticated practice.", "{\"type\":\"object\",\"properties\":{\"serviceTypeId\":{\"type\":\"string\"}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc=JsonDocument.Parse(call.ArgumentsJson); JsonElement a=doc.RootElement; return Result(call, await Scheduling.ResolveServicesAsync(Scope(call,context),a.TryGetProperty("serviceTypeId",out JsonElement id)?id.GetGuid():null,cancellationToken)); }
}
public sealed class ResolveOperationsServiceTypesTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("resolve_operations_service_types", "Resolve service types in the authenticated practice.", "{\"type\":\"object\",\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) => Result(call, await Scheduling.ResolveServiceTypesAsync(Scope(call,context),cancellationToken));
}
public sealed class ResolveOperationsProvidersTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    public override AgentToolDefinition Definition { get; } = new("resolve_operations_providers", "Resolve providers in the authenticated practice.", "{\"type\":\"object\",\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) => Result(call, await Scheduling.ResolveProvidersAsync(Scope(call,context),cancellationToken));
}

public abstract class DirectScheduleTool(IAppointmentSchedulingAgentCapability scheduling) : OperationsTool(scheduling)
{
    protected abstract Task<AgentSchedulingResult> Execute(AgentSchedulingContext context, Guid id, CancellationToken cancellationToken);
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    { using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); return Result(call, await Execute(Scope(call,context), Guid(doc.RootElement,"id"), cancellationToken)); }
}
public sealed class ConfirmOperationTool(IAppointmentSchedulingAgentCapability s) : DirectScheduleTool(s) { public override AgentToolDefinition Definition { get; } = new("operations_confirm_appointment", "Confirm an appointment directly.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"); protected override Task<AgentSchedulingResult> Execute(AgentSchedulingContext c, Guid id, CancellationToken ct)=>Scheduling.ConfirmAppointmentAsync(c,id,ct); }
public sealed class PresentOperationTool(IAppointmentSchedulingAgentCapability s) : DirectScheduleTool(s) { public override AgentToolDefinition Definition { get; } = new("operations_present_appointment", "Mark an appointment present directly.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"); protected override Task<AgentSchedulingResult> Execute(AgentSchedulingContext c, Guid id, CancellationToken ct)=>Scheduling.PresentAppointmentAsync(c,id,ct); }
public sealed class CompleteOperationTool(IAppointmentSchedulingAgentCapability s) : DirectScheduleTool(s) { public override AgentToolDefinition Definition { get; } = new("operations_complete_appointment", "Complete an appointment directly.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"); protected override Task<AgentSchedulingResult> Execute(AgentSchedulingContext c, Guid id, CancellationToken ct)=>Scheduling.CompleteAppointmentAsync(c,id,ct); }
public sealed class ActivateShiftTool(IAppointmentSchedulingAgentCapability s) : DirectScheduleTool(s) { public override AgentToolDefinition Definition { get; } = new("operations_activate_shift", "Activate a shift directly.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"); protected override Task<AgentSchedulingResult> Execute(AgentSchedulingContext c, Guid id, CancellationToken ct)=>Scheduling.ActivateShiftAsync(c,id,ct); }
public sealed class PauseShiftTool(IAppointmentSchedulingAgentCapability s) : DirectScheduleTool(s) { public override AgentToolDefinition Definition { get; } = new("operations_pause_shift", "Pause a shift directly.", "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"); protected override Task<AgentSchedulingResult> Execute(AgentSchedulingContext c, Guid id, CancellationToken ct)=>Scheduling.PauseShiftAsync(c,id,ct); }

public sealed class PrepareScheduleActionTool : OperationsTool, IPatientAgentPreparationTool
{
    private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["prepare_book_appointment"]="book_appointment", ["prepare_reschedule_appointment"]="reschedule_appointment", ["prepare_cancel_appointment"]="cancel_appointment", ["prepare_reject_appointment"]="reject_appointment", ["prepare_create_shift"]="create_shift", ["prepare_edit_shift"]="edit_shift", ["prepare_close_shift"]="close_shift", ["prepare_cancel_shift"]="cancel_shift", ["prepare_archive_shift"]="archive_shift"
    };
    public override AgentToolDefinition Definition { get; }
    private readonly string _actionType;
    private readonly IPatientAgentPatientSearch _search;
    public PrepareScheduleActionTool(IAppointmentSchedulingAgentCapability scheduling, IPatientAgentPatientSearch search, string toolName) : base(scheduling)
    {
        _search = search;
        _actionType = Names[toolName];
        Definition = new(toolName, "Prepare an immutable scheduling action for explicit review; this does not execute it.", Schema(toolName));
    }
    private static string Schema(string toolName) => toolName switch
    {
        "prepare_book_appointment" => "{\"type\":\"object\",\"properties\":{\"candidateToken\":{\"type\":\"string\"},\"shiftId\":{\"type\":\"string\"},\"date\":{\"type\":\"string\"}},\"required\":[\"candidateToken\",\"shiftId\",\"date\"],\"additionalProperties\":false}",
        "prepare_reschedule_appointment" => "{\"type\":\"object\",\"properties\":{\"reservationId\":{\"type\":\"string\"},\"targetShiftId\":{\"type\":\"string\"},\"targetDate\":{\"type\":\"string\"}},\"required\":[\"reservationId\",\"targetShiftId\",\"targetDate\"],\"additionalProperties\":false}",
        "prepare_create_shift" => ShiftSchema(false),
        "prepare_edit_shift" => ShiftSchema(true),
        _ => "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\"}},\"required\":[\"id\"],\"additionalProperties\":false}"
    };
    private static string ShiftSchema(bool edit) => $"{{\"type\":\"object\",\"properties\":{{\"shiftId\":{{\"type\":\"string\"}},\"visibility\":{{\"type\":\"string\"}},\"dayOfWeek\":{{\"type\":\"string\"}},\"startTime\":{{\"type\":\"string\"}},\"endTime\":{{\"type\":\"string\"}},\"timeZone\":{{\"type\":\"string\"}},\"totalSeats\":{{\"type\":\"integer\"}},\"serviceProviderId\":{{\"type\":\"string\"}},\"serviceId\":{{\"type\":\"string\"}},\"bookingOpensBefore\":{{\"type\":\"integer\"}},\"bookingCloseBefore\":{{\"type\":\"integer\"}}}},\"required\":[{(edit ? "\"shiftId\"," : "")}\"visibility\",\"dayOfWeek\",\"startTime\",\"endTime\",\"timeZone\",\"totalSeats\"],\"additionalProperties\":false}}";
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument doc = JsonDocument.Parse(call.ArgumentsJson); JsonElement a = doc.RootElement; AgentSchedulingContext s = Scope(call,context); AgentSchedulingResult<AgentSchedulingProposal> result;
        PatientAgentCandidateReference? patient = _actionType == "book_appointment"
            ? await ResolveCandidate(a, context, cancellationToken)
            : null;
        result = _actionType switch
        {
            "book_appointment" => await Scheduling.PrepareBookAppointmentAsync(s, new(Guid(a,"shiftId"), Date(a,"date"), patient!.PatientId.ToString("D")), cancellationToken),
            "reschedule_appointment" => await Scheduling.PrepareRescheduleAppointmentAsync(s, new(Guid(a,"reservationId"),Guid(a,"targetShiftId"),Date(a,"targetDate")), cancellationToken),
            "cancel_appointment" => await Scheduling.PrepareCancelAppointmentAsync(s, Guid(a,"id"), cancellationToken),
            "reject_appointment" => await Scheduling.PrepareRejectAppointmentAsync(s, Guid(a,"id"), cancellationToken),
            "create_shift" => await Scheduling.PrepareCreateShiftAsync(s, Shift(a,false), cancellationToken),
            "edit_shift" => await Scheduling.PrepareEditShiftAsync(s, Shift(a,true), cancellationToken),
            "close_shift" => await Scheduling.PrepareCloseShiftAsync(s, Guid(a,"id"), cancellationToken),
            "cancel_shift" => await Scheduling.PrepareCancelShiftAsync(s, Guid(a,"id"), cancellationToken),
            "archive_shift" => await Scheduling.PrepareArchiveShiftAsync(s, Guid(a,"id"), cancellationToken),
            _ => throw new InvalidOperationException("Unsupported schedule action.")
        };
        return new AgentToolResult(
            call.Id,
            JsonSerializer.Serialize(
                ToAgentResult(result, patient),
                OperationsAgentToolJsonSerializerContext.Default.OperationsPreparedActionResult));
    }
    private async Task<PatientAgentCandidateReference> ResolveCandidate(JsonElement args, AgentExecutionContext context, CancellationToken ct)
    { if (!args.TryGetProperty("candidateToken", out _)) throw new JsonException("An opaque candidate token is required."); return await _search.ResolveAsync(Required(context,"practiceId"),Required(context,"authenticatedUserId"),context.ExecutionId,Candidate(args),ct) ?? throw new JsonException("The patient candidate is invalid or expired."); }
    public PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        OperationsPreparedActionResult? prepared = JsonSerializer.Deserialize(
            result.Output,
            OperationsAgentToolJsonSerializerContext.Default.OperationsPreparedActionResult);
        return prepared is
        {
            Status: nameof(AgentSchedulingStatus.Prepared),
            ActionId: { } actionId,
            Review: { } review,
            ExpiresAt: { } expiresAt
        } && string.Equals(review.ActionType, _actionType, StringComparison.Ordinal)
            ? new(
                "Operations",
                _actionType,
                actionId,
                JsonSerializer.Serialize(review, OperationsAgentToolJsonSerializerContext.Default.OperationsActionReview),
                expiresAt)
            : null;
    }

    private OperationsPreparedActionResult ToAgentResult(AgentSchedulingResult<AgentSchedulingProposal> result, PatientAgentCandidateReference? patient)
    {
        AgentSchedulingProposal? proposal = result.Value;
        return new(
            result.Status.ToString(),
            proposal?.Id,
            proposal is null ? null : ToReview(proposal, patient),
            proposal?.ExpiresAt,
            result.Message);
    }

    private OperationsActionReview ToReview(AgentSchedulingProposal proposal, PatientAgentCandidateReference? patient) => new(
        _actionType,
        proposal.Id,
        patient is null ? null : new OperationsPatientReview(patient.DisplayName, patient.MaskedIdentification, patient.MaskedPhone),
        proposal.Appointment is { } appointment
            ? new OperationsAppointmentReview(appointment.ShiftId, appointment.Date, appointment.EstimatedSessionLength)
            : null,
        proposal.Reschedule is { } reschedule
            ? new OperationsRescheduleReview(reschedule.ReservationId, reschedule.TargetShiftId, reschedule.TargetDate)
            : null,
        proposal.Shift is { } shift
            ? new OperationsShiftReview(
                shift.Id,
                shift.Visibility.ToString(),
                shift.DayOfWeek.ToString(),
                shift.StartTime,
                shift.EndTime,
                shift.TimeZone,
                shift.TotalSeats,
                shift.ServiceProviderId,
                shift.ServiceId,
                shift.BookingOpensBefore,
                shift.BookingCloseBefore)
            : null,
        proposal.EntityId,
        proposal.ExpectedAppointmentStatus?.ToString(),
        proposal.ExpectedShiftStatus?.ToString(),
        proposal.ExpiresAt);
}

internal sealed record OperationsAgentToolResponse<T>(
    string Status,
    T? Value = default,
    Guid? OperationId = null,
    string? Message = null);

internal sealed record OperationsAppointmentRead(
    Guid Id,
    string Status,
    Guid ShiftId,
    DateOnly Date,
    TimeSpan? EstimatedSessionLength,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    bool IsHold,
    DateTime? ExpiresAt)
{
    public static OperationsAppointmentRead From(Vitarish.Appointments.Domain.Dto.ReservationReadDto appointment) => new(
        appointment.Id,
        appointment.Status.ToString(),
        appointment.ShiftId,
        appointment.Date,
        appointment.EstimatedSessionLength,
        appointment.ConfirmedAt,
        appointment.CancelledAt,
        appointment.IsHold,
        appointment.ExpiresAt);
}

internal sealed record OperationsPreparedActionResult(
    string Status,
    Guid? ActionId,
    OperationsActionReview? Review,
    DateTimeOffset? ExpiresAt,
    string? Message);

internal sealed record OperationsActionReview(
    string ActionType,
    Guid ActionId,
    OperationsPatientReview? Patient,
    OperationsAppointmentReview? Appointment,
    OperationsRescheduleReview? Reschedule,
    OperationsShiftReview? Shift,
    Guid? EntityId,
    string? ExpectedAppointmentStatus,
    string? ExpectedShiftStatus,
    DateTimeOffset ExpiresAt);

internal sealed record OperationsPatientReview(string DisplayName, string MaskedIdentification, string MaskedPhone);

internal sealed record OperationsAppointmentReview(
    Guid ShiftId,
    DateOnly Date,
    TimeSpan? EstimatedSessionLength);

internal sealed record OperationsRescheduleReview(
    Guid ReservationId,
    Guid TargetShiftId,
    DateOnly TargetDate);

internal sealed record OperationsShiftReview(
    Guid Id,
    string Visibility,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TimeZone,
    int TotalSeats,
    Guid? ServiceProviderId,
    Guid? ServiceId,
    int? BookingOpensBefore,
    int? BookingCloseBefore);
