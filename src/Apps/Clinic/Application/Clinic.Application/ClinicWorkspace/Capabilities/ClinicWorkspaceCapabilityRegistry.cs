using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public sealed class ClinicWorkspaceCapabilityRegistry
{
    private readonly IReadOnlyCollection<IClinicWorkspaceCapability> _capabilities;
    private readonly IReadOnlyDictionary<string, ClinicWorkspaceActionPolicy> _actionPolicies;
    private readonly IReadOnlyDictionary<string, ClinicWorkspaceActionPolicy> _toolPolicies;
    private readonly ClinicWorkspaceAuthorizationPolicy _authorizationPolicy;
    private readonly ClinicWorkspaceToolAvailabilityPolicy _availabilityPolicy;

    public ClinicWorkspaceCapabilityRegistry(
        IEnumerable<IClinicWorkspaceCapability> capabilities,
        ClinicWorkspaceAuthorizationPolicy authorizationPolicy,
        ClinicWorkspaceToolAvailabilityPolicy availabilityPolicy,
        IEnumerable<IClinicWorkspaceActionCommitter> committers)
    {
        _authorizationPolicy = authorizationPolicy;
        _availabilityPolicy = availabilityPolicy;
        _capabilities = capabilities.ToArray();
        (_actionPolicies, _toolPolicies) = ValidateRegistrations(_capabilities, committers);
    }

    public IReadOnlyCollection<IClinicWorkspaceCapability> GetAvailable(ClinicWorkspaceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _capabilities
            .Where(capability => _availabilityPolicy.IsAvailable(capability, context))
            .Where(capability => _authorizationPolicy.IsAuthorized(capability, context))
            .ToArray();
    }

    public ClinicWorkspaceActionPolicy? ResolveAction(string actionType) =>
        _actionPolicies.GetValueOrDefault(actionType);

    public ClinicWorkspaceActionPolicy? ResolveTool(string toolName) =>
        _toolPolicies.GetValueOrDefault(toolName);

    public IReadOnlyCollection<ClinicWorkspaceActionPolicy> Policies => _actionPolicies.Values.ToArray();

    private static (IReadOnlyDictionary<string, ClinicWorkspaceActionPolicy> Actions, IReadOnlyDictionary<string, ClinicWorkspaceActionPolicy> Tools) ValidateRegistrations(
        IReadOnlyCollection<IClinicWorkspaceCapability> capabilities,
        IEnumerable<IClinicWorkspaceActionCommitter> committers)
    {
        if (capabilities.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != capabilities.Count)
        {
            throw new InvalidOperationException("Clinic assistant capability names must be unique.");
        }

        var tools = capabilities.SelectMany(item => item.Tools).ToArray();
        if (tools.Select(tool => tool.Definition.Name).Distinct(StringComparer.Ordinal).Count() != tools.Length)
        {
            throw new InvalidOperationException("Clinic assistant tool names must be stable and unique.");
        }

        Dictionary<string, ClinicWorkspaceActionPolicy> actions = new(StringComparer.Ordinal);
        Dictionary<string, ClinicWorkspaceActionPolicy> actionTools = new(StringComparer.Ordinal);
        foreach (IClinicWorkspaceCapability capability in capabilities)
        {
            HashSet<string> declaredTypes = capability.ActionTypes.ToHashSet(StringComparer.Ordinal);
            if (declaredTypes.Count != capability.ActionPolicies.Count ||
                !declaredTypes.SetEquals(capability.ActionPolicies.Select(policy => policy.ActionType)))
                throw new InvalidOperationException($"Capability '{capability.Name}' action types and action policies drifted.");

            foreach (ClinicWorkspaceActionPolicy policy in capability.ActionPolicies)
            {
                if (string.IsNullOrWhiteSpace(policy.ActionType) || string.IsNullOrWhiteSpace(policy.OwningWorkflow) ||
                    policy.Revision < 1 || policy.RequiredPermissions is null || policy.RequiredRoles is null ||
                    policy.ToolNames is null ||
                    policy.RequiredPermissions.Any(string.IsNullOrWhiteSpace) ||
                    policy.RequiredRoles.Any(string.IsNullOrWhiteSpace) ||
                    policy.ToolNames.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException($"Action policy '{policy.ActionType}' has invalid ownership, authorization, revision, or retry metadata.");
                if (!Enum.IsDefined(policy.ExecutionMode) || !Enum.IsDefined(policy.RetryClassification))
                    throw new InvalidOperationException($"Action policy '{policy.ActionType}' has an unsupported execution or retry classification.");
                if (!actions.TryAdd(policy.ActionType, policy))
                    throw new InvalidOperationException($"Clinic assistant action type '{policy.ActionType}' has multiple policies.");
                if (policy.ExecutionMode == ClinicWorkspaceActionExecutionMode.PrepareAndConfirm &&
                    (policy.DefaultProposalLifetime is not { } lifetime || lifetime <= TimeSpan.Zero))
                    throw new InvalidOperationException($"Prepared action '{policy.ActionType}' requires a positive proposal lifetime.");
                if (policy.ExecutionMode == ClinicWorkspaceActionExecutionMode.DirectAllowlisted && policy.DefaultProposalLifetime is not null)
                    throw new InvalidOperationException($"Direct action '{policy.ActionType}' cannot have a proposal lifetime.");
                if (policy.ToolNames.Count == 0)
                    throw new InvalidOperationException($"Action '{policy.ActionType}' must declare at least one executable tool.");

                HashSet<string> capabilityToolNames = capability.Tools.Select(tool => tool.Definition.Name).ToHashSet(StringComparer.Ordinal);
                foreach (string toolName in policy.ToolNames)
                {
                    if (!capabilityToolNames.Contains(toolName))
                        throw new InvalidOperationException($"Action policy '{policy.ActionType}' references an unregistered tool '{toolName}'.");
                    IAgentTool policyTool = capability.Tools.Single(tool => string.Equals(tool.Definition.Name, toolName, StringComparison.Ordinal));
                    if (policy.RequiresConfirmation && policyTool is not IClinicWorkspacePreparationTool)
                        throw new InvalidOperationException($"Prepared action '{policy.ActionType}' must reference a preparation tool.");
                    if (!policy.RequiresConfirmation && policyTool is IClinicWorkspacePreparationTool)
                        throw new InvalidOperationException($"Direct action '{policy.ActionType}' cannot reference a preparation tool.");
                    if (!actionTools.TryAdd(toolName, policy))
                        throw new InvalidOperationException($"Clinic assistant tool '{toolName}' has multiple action policies.");
                }
            }
        }

        foreach (var tool in tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Definition.Name))
            {
                throw new InvalidOperationException("A clinic assistant tool name is required.");
            }

            using JsonDocument schema = JsonDocument.Parse(tool.Definition.ParametersJsonSchema);
            if (schema.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"Clinic assistant tool '{tool.Definition.Name}' must declare an object schema.");
            if (ContainsPatientIdProperty(schema.RootElement))
            {
                throw new InvalidOperationException("Clinic assistant tools must use the route-scoped patient and cannot accept patientId.");
            }
        }

        Dictionary<string, IClinicWorkspaceActionCommitter> committedActions = new(StringComparer.Ordinal);
        foreach (IClinicWorkspaceActionCommitter committer in committers)
        {
            if (string.IsNullOrWhiteSpace(committer.WorkflowKey) || committer.ActionTypes is null ||
                committer.ActionTypes.Count == 0 || committer.ActionTypes.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Clinic assistant committers require a workflow owner and at least one action type.");
            foreach (string actionType in committer.ActionTypes)
            {
                if (!committedActions.TryAdd(actionType, committer))
                    throw new InvalidOperationException($"Clinic assistant action type '{actionType}' has multiple committers.");
                if (!actions.TryGetValue(actionType, out ClinicWorkspaceActionPolicy? policy))
                    throw new InvalidOperationException($"Committer '{committer.WorkflowKey}' owns undeclared action '{actionType}'.");
                if (policy.ExecutionMode != ClinicWorkspaceActionExecutionMode.PrepareAndConfirm)
                    throw new InvalidOperationException($"Direct action '{actionType}' cannot register a confirmation committer.");
                if (!string.Equals(policy.OwningWorkflow, committer.WorkflowKey, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Action '{actionType}' policy owner and committer workflow differ.");
            }
        }

        foreach (ClinicWorkspaceActionPolicy policy in actions.Values.Where(policy => policy.RequiresConfirmation))
        {
            if (!committedActions.ContainsKey(policy.ActionType))
                throw new InvalidOperationException($"Prepared action '{policy.ActionType}' has no committer.");
        }

        return (actions, actionTools);
    }

    private static bool ContainsPatientIdProperty(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Any(property =>
            string.Equals(property.Name, "patientId", StringComparison.OrdinalIgnoreCase) || ContainsPatientIdProperty(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Any(ContainsPatientIdProperty),
        _ => false
    };
}
