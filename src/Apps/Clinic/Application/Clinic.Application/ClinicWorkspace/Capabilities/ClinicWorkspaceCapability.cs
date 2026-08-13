using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public abstract class ClinicWorkspaceCapability(
    IReadOnlyCollection<IAgentTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : IClinicWorkspaceCapability
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlySet<string> RequiredPermissions { get; }
    public IReadOnlyCollection<IAgentTool> Tools { get; } = tools;
    public IReadOnlyCollection<string> ActionTypes { get; } = actionTypes ?? [];
    public virtual bool IsAvailable(ClinicWorkspaceContext context) => context.SubjectKind == "patient";
    public virtual string GetInstructions() => Description;
}

public interface IAppointmentsClinicWorkspaceTool : IAgentTool;
public interface IPrescriptionsClinicWorkspaceTool : IAgentTool;
public interface IClinicalEventsClinicWorkspaceTool : IAgentTool;

public sealed class AppointmentsClinicWorkspaceCapability(
    IEnumerable<IAppointmentsClinicWorkspaceTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : ClinicWorkspaceCapability([.. tools], actionTypes ?? ["create_appointment"])
{
    public override string Name => "Appointments";
    public override string Description => "Review and prepare patient appointment operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read" };
}

public sealed class PrescriptionsClinicWorkspaceCapability(
    IEnumerable<IPrescriptionsClinicWorkspaceTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : ClinicWorkspaceCapability([.. tools], actionTypes ?? ["create_prescription"])
{
    public override string Name => "Prescriptions";
    public override string Description => "Review and prepare patient prescription operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read" };
}
