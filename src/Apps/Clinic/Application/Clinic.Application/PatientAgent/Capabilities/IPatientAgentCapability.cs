using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities;

public interface IPatientAgentCapability
{
    string Name { get; }
    string Description { get; }
    string GetInstructions();
    IReadOnlySet<string> RequiredPermissions { get; }
    IReadOnlyCollection<IAgentTool> Tools { get; }
    IReadOnlyCollection<string> ActionTypes { get; }
    bool IsAvailable(PatientAgentContext context);
}
