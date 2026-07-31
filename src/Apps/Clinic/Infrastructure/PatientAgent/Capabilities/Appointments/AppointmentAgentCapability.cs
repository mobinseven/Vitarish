using System.Text.Json;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Appointments;

public sealed class AppointmentAgentCapability(
    CheckAppointmentAvailabilityTool check,
    PrepareAppointmentTool prepare,
    GetNextPatientAppointmentTool next,
    ListPatientAppointmentsTool list,
    CountPatientAppointmentsTool count) : PatientAgentCapability([check, prepare, next, list, count], ["create_appointment"])
{
    public override string Name => "Appointments";
    public override string Description => "Check appointment availability and prepare an exact proposal for later explicit confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read", "Appointments.Create" };
    public override string GetInstructions() =>
        "Use appointment query tools for existing appointments. Ask for a local date and time, duration, and appointment type or service when preparing a new appointment; do not commit it.";
}

public sealed class PatientAppointmentQueryService(IReservationManager reservations, IShiftManager shifts)
{
    private static readonly HashSet<ReservationStatus> Excluded = [ReservationStatus.Cancelled, ReservationStatus.Rejected, ReservationStatus.Expired];

    public async Task<IReadOnlyList<PatientAppointmentSummary>> QueryAsync(
        AgentExecutionContext context, bool future, int take, CancellationToken cancellationToken)
    {
        IReadOnlyList<PatientAppointmentSummary> appointments =
            await QueryAllAsync(context, future, cancellationToken);

        return (future
            ? appointments.OrderBy(item => item.Date).ThenBy(item => item.StartTime)
            : appointments.OrderByDescending(item => item.Date).ThenByDescending(item => item.StartTime))
            .Take(Math.Clamp(take, 1, 100)).ToArray();
    }

    public async Task<int> CountAsync(AgentExecutionContext context, bool future, CancellationToken cancellationToken)
        => (await QueryAllAsync(context, future, cancellationToken)).Count;

    private async Task<IReadOnlyList<PatientAppointmentSummary>> QueryAllAsync(
        AgentExecutionContext context, bool future, CancellationToken cancellationToken)
    {
        if (context.Metadata is null ||
            !context.Metadata.TryGetValue("patientId", out string? patientId) ||
            !Guid.TryParse(patientId, out _))
            throw new InvalidOperationException("The trusted patient appointment scope is incomplete.");

        DateOnly today = Today(context);
        ReservationDescriptorDto descriptor = new()
        {
            ReservingPartyId = patientId,
            DateFrom = future ? today : null,
            DateTo = future ? null : today,
            Skip = 0,
            // Classification depends on the shift's local start time, so pagination
            // must happen after reservations and shifts have been joined.
            Take = int.MaxValue
        };

        EntityResult<IReadOnlyList<ReservationReadDto>> result =
            await reservations.GetListAsync(descriptor, cancellationToken);
        if (result.IsFailure || result.Value is null)
            throw new InvalidOperationException("Patient appointments could not be loaded.");

        List<PatientAppointmentSummary> appointments = [];
        foreach (ReservationReadDto reservation in result.Value.Where(item => !Excluded.Contains(item.Status)))
        {
            EntityResult<ShiftReadDto> shift = await shifts.GetAsync(reservation.ShiftId, cancellationToken);
            if (shift.IsFailure || shift.Value is null)
            {
                // Historical reservations can outlive a deleted shift. Their date
                // remains useful, but an exact time must not be invented.
                if (reservation.Date == today || (reservation.Date > today) != future) continue;
                appointments.Add(new(
                    reservation.Id, reservation.Date, null, null, null,
                    reservation.Status.ToString(), reservation.ShiftId, false));
                continue;
            }
            if (IsFuture(reservation, shift.Value, context) != future) continue;

            appointments.Add(new(
                reservation.Id, reservation.Date, shift.Value.StartTime, shift.Value.EndTime,
                shift.Value.TimeZone, reservation.Status.ToString(), reservation.ShiftId, true));
        }

        return appointments;
    }

    private static DateOnly Today(AgentExecutionContext context)
    {
        string zoneId = context.Metadata?.GetValueOrDefault("timeZone") ?? "UTC";
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
        DateTimeOffset now = context.Metadata?.TryGetValue("currentUtc", out string? value) == true &&
            DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : DateTimeOffset.UtcNow;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
    }

    private static bool IsFuture(ReservationReadDto reservation, ShiftReadDto shift, AgentExecutionContext context)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(shift.TimeZone); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
        DateTime localStart = reservation.Date.ToDateTime(shift.StartTime);
        DateTimeOffset start = new(localStart, zone.GetUtcOffset(localStart));
        DateTimeOffset now = context.Metadata?.TryGetValue("currentUtc", out string? value) == true &&
            DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : DateTimeOffset.UtcNow;
        return start > now;
    }
}

public sealed record PatientAppointmentSummary(
    Guid ReservationId, DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime,
    string? TimeZone, string Status, Guid ShiftId, bool ScheduleAvailable);

public abstract class AppointmentQueryTool(PatientAppointmentQueryService queries) : IAppointmentsPatientAgentTool
{
    protected PatientAppointmentQueryService Queries { get; } = queries;
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);
    protected static bool Future(JsonElement args) =>
        !args.TryGetProperty("period", out JsonElement period) ||
        string.Equals(period.GetString(), "future", StringComparison.OrdinalIgnoreCase);
    protected static int Limit(JsonElement args) =>
        args.TryGetProperty("limit", out JsonElement limit) && limit.TryGetInt32(out int value) ? value : 20;
    protected static AgentToolResult Result(AgentToolCall call, object? value) =>
        new(call.Id, JsonSerializer.Serialize(value));
}

public sealed class GetNextPatientAppointmentTool(PatientAppointmentQueryService queries) : AppointmentQueryTool(queries)
{
    public override AgentToolDefinition Definition { get; } = new("get_next_patient_appointment", "Get the current patient's next scheduled appointment.", "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatientAppointmentSummary> appointments = await Queries.QueryAsync(context, true, 1, cancellationToken);
        return Result(call, new { found = appointments.Count > 0, appointment = appointments.FirstOrDefault() });
    }
}

public sealed class ListPatientAppointmentsTool(PatientAppointmentQueryService queries) : AppointmentQueryTool(queries)
{
    private const string Schema = "{\"type\":\"object\",\"properties\":{\"period\":{\"type\":\"string\",\"enum\":[\"future\",\"past\"]},\"limit\":{\"type\":\"integer\"}},\"additionalProperties\":false}";
    public override AgentToolDefinition Definition { get; } = new("list_patient_appointments", "List the current patient's scheduled appointments in the past or future.", Schema);
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        bool future = Future(args.RootElement);
        return Result(call, new { period = future ? "future" : "past", appointments = await Queries.QueryAsync(context, future, Limit(args.RootElement), cancellationToken) });
    }
}

public sealed class CountPatientAppointmentsTool(PatientAppointmentQueryService queries) : AppointmentQueryTool(queries)
{
    public override AgentToolDefinition Definition { get; } = new("count_patient_appointments", "Count the current patient's scheduled appointments in the past or future.", "{\"type\":\"object\",\"properties\":{\"period\":{\"type\":\"string\",\"enum\":[\"future\",\"past\"]}},\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        bool future = Future(args.RootElement);
        int count = await Queries.CountAsync(context, future, cancellationToken);
        return Result(call, new { period = future ? "future" : "past", count });
    }
}

public abstract class AppointmentTool(IPatientAppointmentAgentCapability capability) : IAppointmentsPatientAgentTool
{
    protected IPatientAppointmentAgentCapability Capability { get; } = capability;
    public abstract AgentToolDefinition Definition { get; }
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default);

    protected static (Guid PatientId, string PracticeId, string TimeZone) Scope(AgentExecutionContext context)
    {
        if (context.Metadata is null ||
            !context.Metadata.TryGetValue("patientId", out string? patient) || !Guid.TryParse(patient, out Guid patientId) ||
            !context.Metadata.TryGetValue("practiceId", out string? practiceId) ||
            !context.Metadata.TryGetValue("timeZone", out string? timeZone))
            throw new InvalidOperationException("The trusted patient appointment scope is incomplete.");
        return (patientId, practiceId, timeZone);
    }

    protected static PatientAppointmentRequest Request(JsonElement args, AgentExecutionContext context)
    {
        var scope = Scope(context);
        return new(scope.PatientId, scope.PracticeId,
            args.GetProperty("requestedLocalDateTime").GetString()!,
            args.TryGetProperty("durationMinutes", out JsonElement duration) ? duration.GetInt32() : null,
            String(args, "appointmentType"), String(args, "servicePreference"),
            String(args, "practitionerPreference"), String(args, "description"), scope.TimeZone);
    }

    protected static AgentToolResult Result(AgentToolCall call, object? value) =>
        new(call.Id, JsonSerializer.Serialize(value));
    private static string? String(JsonElement value, string name) =>
        value.TryGetProperty(name, out JsonElement property) ? property.GetString() : null;

    protected const string RequestSchema = """
        {"type":"object","properties":{"requestedLocalDateTime":{"type":"string"},"durationMinutes":{"type":"integer"},"appointmentType":{"type":"string"},"servicePreference":{"type":"string"},"practitionerPreference":{"type":"string"},"description":{"type":"string"}},"required":["requestedLocalDateTime"],"additionalProperties":false}
        """;
}

public sealed class CheckAppointmentAvailabilityTool(IPatientAppointmentAgentCapability capability) : AppointmentTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("check_appointment_availability", "Check matching availability for the current patient and practice.", RequestSchema);
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        return Result(call, await Capability.CheckAvailabilityAsync(Request(args.RootElement, context), cancellationToken));
    }
}

public sealed class PrepareAppointmentTool(IPatientAppointmentAgentCapability capability) : AppointmentTool(capability), IPatientAgentPreparationTool
{
    public override AgentToolDefinition Definition { get; } = new("prepare_appointment", "Prepare an immutable appointment proposal for explicit review; this does not book.", RequestSchema);
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        return Result(call, await Capability.PrepareAsync(Request(args.RootElement, context), cancellationToken));
    }

    public PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        AppointmentProposalResult? preparation = JsonSerializer.Deserialize<AppointmentProposalResult>(result.Output);
        return preparation is { Status: AppointmentProposalResultStatus.Prepared, Proposal: { } proposal }
            ? new("Appointments", "create_appointment", proposal.Id, JsonSerializer.Serialize(proposal), proposal.ExpiresAt)
            : null;
    }
}

public sealed class ConfirmAppointmentTool(IPatientAppointmentAgentCapability capability) : AppointmentTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("confirm_appointment", "Confirm a previously reviewed immutable appointment proposal.",
        "{\"type\":\"object\",\"properties\":{\"proposalId\":{\"type\":\"string\"}},\"required\":[\"proposalId\"],\"additionalProperties\":false}");
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        Guid proposalId = args.RootElement.GetProperty("proposalId").GetGuid();
        var scope = Scope(context);
        return Result(call, await Capability.ConfirmAsync(proposalId, scope.PatientId, scope.PracticeId, cancellationToken));
    }
}
