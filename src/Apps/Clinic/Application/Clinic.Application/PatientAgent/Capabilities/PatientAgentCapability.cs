using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities;

public abstract class PatientAgentCapability(
    IReadOnlyCollection<IAgentTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : IPatientAgentCapability
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlySet<string> RequiredPermissions { get; }
    public IReadOnlyCollection<IAgentTool> Tools { get; } = tools;
    public IReadOnlyCollection<string> ActionTypes { get; } = actionTypes ?? [];
    public virtual bool IsAvailable(PatientAgentContext context) => true;
    public virtual string GetInstructions() => Description;
}

public interface IAppointmentsPatientAgentTool : IAgentTool;
public interface IPrescriptionsPatientAgentTool : IAgentTool;
public interface IClinicalEventsPatientAgentTool : IAgentTool;

public sealed class AppointmentsPatientAgentCapability(
    IEnumerable<IAppointmentsPatientAgentTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : PatientAgentCapability([.. tools], actionTypes ?? ["create_appointment"])
{
    public override string Name => "Appointments";
    public override string Description => "Review and prepare patient appointment operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read" };
}

public sealed class PrescriptionsPatientAgentCapability(
    IEnumerable<IPrescriptionsPatientAgentTool> tools,
    IReadOnlyCollection<string>? actionTypes = null) : PatientAgentCapability([.. tools], actionTypes ?? ["create_prescription"])
{
    public override string Name => "Prescriptions";
    public override string Description => "Review and prepare patient prescription operations.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "Prescriptions.Read" };
}
