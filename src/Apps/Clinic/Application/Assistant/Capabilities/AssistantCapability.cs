using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant.Capabilities;

public abstract class AssistantCapability(
    IReadOnlyCollection<IAgentTool> tools,
    IReadOnlyCollection<AssistantActionPolicy>? actionPolicies = null) : IAssistantCapability
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlySet<string> RequiredPermissions { get; }
    public IReadOnlyCollection<IAgentTool> Tools { get; } = tools;
    public IReadOnlyCollection<AssistantActionPolicy> ActionPolicies { get; } = actionPolicies ?? [];
    public virtual bool IsAvailable(AssistantContext context) => context.SubjectKind == "patient";
    public virtual string GetInstructions() => Description;
}

public interface IAppointmentsAssistantTool : IAgentTool;
public interface IPrescriptionsAssistantTool : IAgentTool;
public interface IClinicalEventsAssistantTool : IAgentTool;

public sealed class AppointmentsAssistantCapability(
    IEnumerable<IAppointmentsAssistantTool> tools,
    IReadOnlyCollection<AssistantActionPolicy>? actionPolicies = null) : AssistantCapability(
        [.. tools], actionPolicies ?? [AssistantActionPolicies.Prepare("create_appointment", "appointments", ["Appointments.Create"], toolNames: ["prepare_appointment"], retryClassification: AssistantActionRetryClassification.Retryable)])
{
    public override string Name => "Appointments";
    public override string Description => "Review and prepare patient appointment operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read" };
}

public sealed class PrescriptionsAssistantCapability(
    IEnumerable<IPrescriptionsAssistantTool> tools,
    IReadOnlyCollection<AssistantActionPolicy>? actionPolicies = null) : AssistantCapability(
        [.. tools], actionPolicies ?? [AssistantActionPolicies.Prepare("create_prescription", "prescriptions", ["Prescriptions.Create"], toolNames: ["prepare_prescription"])])
{
    public override string Name => "Prescriptions";
    public override string Description => "Review and prepare patient prescription operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read" };
}
