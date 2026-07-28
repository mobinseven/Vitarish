using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public interface IPatientAgentCapability
{
    string Name { get; }
    string Description { get; }
    IReadOnlySet<string> RequiredPermissions { get; }
    IReadOnlyCollection<IAgentTool> ContextTools { get; }
    IReadOnlyCollection<string> ActionTypes { get; }
}
