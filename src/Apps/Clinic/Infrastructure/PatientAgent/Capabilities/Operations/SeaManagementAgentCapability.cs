using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;

/// <summary>Clinic composition adapter for the SEA-owned management capability.</summary>
public sealed class SeaManagementAgentCapability(SeaManagementClinicToolAdapter tool) : PatientAgentCapability([tool], ["sea_manage"])
{
    public override string Name => "SeaManagement";
    public override string Description => "Operate the authenticated practice's SEA projects, procedures, events, attachments, receipts, roles, and statistics.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(PatientAgentContext context) => context.SubjectKind == "practice" && PatientAgentOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() => "Use sea_manage for authenticated-practice SEA operations. Mutations require confirmation.";
}

public sealed class SeaManagementClinicToolAdapter(ISeaManagementAgentTool inner) : IPatientAgentPreparationTool
{
    public AgentToolDefinition Definition => inner.Definition;

    public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        inner.ExecuteAsync(call, context, cancellationToken);

    public PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        SeaManagementPreparedAction? action = inner.ParsePreparedAction(result);
        return action is null
            ? null
            : new("SeaManagement", "sea_manage", action.ActionId, JsonSerializer.Serialize(action), action.ExpiresAt);
    }
}
