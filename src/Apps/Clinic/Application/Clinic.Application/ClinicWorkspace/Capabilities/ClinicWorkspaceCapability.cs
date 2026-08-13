using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public abstract class ClinicWorkspaceCapability(
    IReadOnlyCollection<IAgentTool> tools,
    IReadOnlyCollection<ClinicWorkspaceActionPolicy>? actionPolicies = null) : IClinicWorkspaceCapability
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlySet<string> RequiredPermissions { get; }
    public IReadOnlyCollection<IAgentTool> Tools { get; } = tools;
    public IReadOnlyCollection<ClinicWorkspaceActionPolicy> ActionPolicies { get; } = actionPolicies ?? [];
    public virtual bool IsAvailable(ClinicWorkspaceContext context) => context.SubjectKind == "patient";
    public virtual string GetInstructions() => Description;
}

public interface IAppointmentsClinicWorkspaceTool : IAgentTool;
public interface IPrescriptionsClinicWorkspaceTool : IAgentTool;
public interface IClinicalEventsClinicWorkspaceTool : IAgentTool;

public sealed class AppointmentsClinicWorkspaceCapability(
    IEnumerable<IAppointmentsClinicWorkspaceTool> tools,
    IReadOnlyCollection<ClinicWorkspaceActionPolicy>? actionPolicies = null) : ClinicWorkspaceCapability(
        [.. tools], actionPolicies ?? [ClinicWorkspaceActionPolicies.Prepare("create_appointment", "appointments", ["Appointments.Create"], toolNames: ["prepare_appointment"], retryClassification: ClinicWorkspaceActionRetryClassification.Retryable)])
{
    public override string Name => "Appointments";
    public override string Description => "Review and prepare patient appointment operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read" };
}

public sealed class PrescriptionsClinicWorkspaceCapability(
    IEnumerable<IPrescriptionsClinicWorkspaceTool> tools,
    IReadOnlyCollection<ClinicWorkspaceActionPolicy>? actionPolicies = null) : ClinicWorkspaceCapability(
        [.. tools], actionPolicies ?? [ClinicWorkspaceActionPolicies.Prepare("create_prescription", "prescriptions", ["Prescriptions.Create"], toolNames: ["prepare_prescription"])])
{
    public override string Name => "Prescriptions";
    public override string Description => "Review and prepare patient prescription operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read" };
}
