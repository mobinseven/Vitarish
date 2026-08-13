using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant.Capabilities;

public interface IAssistantCapability
{
    string Name { get; }
    string Description { get; }
    string GetInstructions();
    IReadOnlySet<string> RequiredPermissions { get; }
    IReadOnlyCollection<IAgentTool> Tools { get; }
    IReadOnlyCollection<AssistantActionPolicy> ActionPolicies { get; }
    IReadOnlyCollection<string> ActionTypes => ActionPolicies.Select(policy => policy.ActionType).ToArray();
    bool IsAvailable(AssistantContext context);
}
