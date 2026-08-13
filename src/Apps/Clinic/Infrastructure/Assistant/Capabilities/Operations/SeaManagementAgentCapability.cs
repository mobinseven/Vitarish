using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;

/// <summary>Clinic composition adapter for the SEA-owned management capability.</summary>
public sealed class SeaManagementAgentCapability(SeaManagementClinicToolAdapter tool) : AssistantCapability([tool], [
    AssistantActionPolicies.Prepare("sea_manage", "sea-management", [],
        requiredRoles: [AssistantOperationsScope.TenantOwnerRole], toolNames: ["sea_manage"])
])
{
    public override string Name => "SeaManagement";
    public override string Description => "Operate the authenticated practice's SEA projects, procedures, events, attachments, receipts, roles, and statistics.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) => context.SubjectKind == "practice" && AssistantOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() => "Use sea_manage for authenticated-practice SEA operations. Mutations require confirmation.";
}

public sealed class SeaManagementClinicToolAdapter(ISeaManagementAgentTool inner) : IAssistantPreparationTool
{
    public AgentToolDefinition Definition => inner.Definition;

    public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        inner.ExecuteAsync(call, context, cancellationToken);

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        SeaManagementPreparedAction? action = inner.ParsePreparedAction(result);
        return action is null
            ? null
            : new("SeaManagement", "sea_manage", action.ActionId, JsonSerializer.Serialize(action), action.ExpiresAt);
    }
}
