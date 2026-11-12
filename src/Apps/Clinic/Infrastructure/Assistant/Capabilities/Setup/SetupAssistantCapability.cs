using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup;

/// <summary>
/// Guides a new clinic's owner through setup: the assistant reads the setup goals and the clinic's working hours,
/// works on the next open goal, and proposes a whole step for review. It never commits anything itself.
/// </summary>
public sealed class SetupAssistantCapability(GetClinicSetupProgressTool tool) : AssistantCapability([tool])
{
    public override string Name => ClinicSetupAssistant.CapabilityName;
    public override string Description => "Guide the clinic owner through setting up the clinic.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);

    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == AssistantSubjects.PracticeKind && AssistantOperationsScope.IsAuthorizedRole(context.Role);

    public override string GetInstructions() =>
        $"""
        When the owner is setting up the clinic, call {GetClinicSetupProgressTool.ToolName} first and work on its nextGoal.
        Goals, in order: {nameof(ClinicSetupGoal.ServicesAndProviders)} (at least one service and one provider),
        {nameof(ClinicSetupGoal.ShiftsAndBookingRules)} (at least one active shift and one booking policy),
        {nameof(ClinicSetupGoal.StaffUsers)} (invite at least one staff member), {nameof(ClinicSetupGoal.ClinicProfile)}
        (address and logo, edited by the owner in clinic settings).
        Propose a whole step in one reply: prepare every needed item with the preparing tools (for example one shift per
        working day seeded from the clinic's working hours with appointment_manage in prepare mode), and never commit.
        The owner reviews and confirms everything you prepare.
        End each setup reply with a question whose options are the owner's sensible next choices, and set nextAction to
        the goal whose screen the owner may want to open.
        """;
}

/// <summary>Reads how far the clinic is with setup, plus its working hours, for the assistant. Read-only.</summary>
public sealed class GetClinicSetupProgressTool(ISender sender) : IAgentTool
{
    public const string ToolName = "get_clinic_setup_progress";

    public AgentToolDefinition Definition { get; } = new(
        ToolName,
        "Read the clinic's setup goals (done or open, in order), the next open goal, and the clinic's working days and hours.",
        """{"type":"object","properties":{},"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ClinicSetupProgress progress = await sender.Send(new GetClinicSetupProgressQuery(), cancellationToken);
        TenantBusinessProfileView? profile = await sender.Send(new GetTenantBusinessProfileQuery(), cancellationToken);
        SetupProgressToolResult result = new(
            progress.Goals.Select(goal => new SetupGoalToolResult(goal.Goal.ToString(), goal.State == ClinicSetupGoalState.Done)).ToArray(),
            progress.NextGoal?.ToString(),
            (profile?.Details?.WorkingHours ?? [])
                .Select(day => new WorkingDayToolResult(
                    day.Day.ToString(),
                    day.Intervals.Select(interval => new OpeningIntervalToolResult(interval.Opens.ToString("HH:mm"), interval.Closes.ToString("HH:mm"))).ToArray()))
                .ToArray());
        return new(call.Id, JsonSerializer.Serialize(result, SetupToolJsonSerializerContext.Default.SetupProgressToolResult));
    }
}

public sealed record SetupProgressToolResult(IReadOnlyList<SetupGoalToolResult> Goals, string? NextGoal, IReadOnlyList<WorkingDayToolResult> WorkingHours);

public sealed record SetupGoalToolResult(string Goal, bool Done);

public sealed record WorkingDayToolResult(string Day, IReadOnlyList<OpeningIntervalToolResult> Intervals);

public sealed record OpeningIntervalToolResult(string Opens, string Closes);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SetupProgressToolResult))]
internal sealed partial class SetupToolJsonSerializerContext : JsonSerializerContext;
