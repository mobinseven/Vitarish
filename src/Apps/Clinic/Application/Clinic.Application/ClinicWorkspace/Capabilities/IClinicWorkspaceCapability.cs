using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public interface IClinicWorkspaceCapability
{
    string Name { get; }
    string Description { get; }
    string GetInstructions();
    IReadOnlySet<string> RequiredPermissions { get; }
    IReadOnlyCollection<IAgentTool> Tools { get; }
    IReadOnlyCollection<string> ActionTypes { get; }
    bool IsAvailable(ClinicWorkspaceContext context);
}
