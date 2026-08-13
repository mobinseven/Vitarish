using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public interface IClinicWorkspaceCapability
{
    string Name { get; }
    string Description { get; }
    string GetInstructions();
    IReadOnlySet<string> RequiredPermissions { get; }
    IReadOnlyCollection<IAgentTool> Tools { get; }
    IReadOnlyCollection<ClinicWorkspaceActionPolicy> ActionPolicies { get; }
    IReadOnlyCollection<string> ActionTypes => ActionPolicies.Select(policy => policy.ActionType).ToArray();
    bool IsAvailable(ClinicWorkspaceContext context);
}
