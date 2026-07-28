using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Interface.AgentCapabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities.Appointments;

public sealed class AppointmentAgentCapability(
    CheckAppointmentAvailabilityTool check,
    PrepareAppointmentTool prepare,
    ConfirmAppointmentTool confirm) : PatientAgentCapability([check, prepare, confirm])
{
    public override string Name => "Appointments";
    public override string Description => "Check appointment availability, prepare an exact proposal for review, and confirm it only after explicit patient-scoped approval.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read", "Appointments.Create" };
    public override string GetInstructions() =>
        "Ask for a local date and time, duration, and appointment type or service. Prepare and show the exact proposal. " +
        "Call confirm_appointment only after explicit confirmation; confirmation accepts only the immutable proposal id.";
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

    protected static AgentToolResult Result(AgentToolCall call, object value) =>
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

public sealed class PrepareAppointmentTool(IPatientAppointmentAgentCapability capability) : AppointmentTool(capability)
{
    public override AgentToolDefinition Definition { get; } = new("prepare_appointment", "Prepare an immutable appointment proposal for explicit review; this does not book.", RequestSchema);
    public override async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument args = JsonDocument.Parse(call.ArgumentsJson);
        return Result(call, await Capability.PrepareAsync(Request(args.RootElement, context), cancellationToken));
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
