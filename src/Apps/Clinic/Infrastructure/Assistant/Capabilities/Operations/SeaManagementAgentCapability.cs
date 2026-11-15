using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;
using Names = Vitarish.Clinic.Application.Assistant.Presentation.ClinicPresentableToolNames;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;

/// <summary>
/// Care-team access and practice statistics through the SEA management capability. Patients, procedures and steps are
/// worked on through the typed procedure tools instead, so only access-role and statistics operations are offered here.
/// </summary>
public sealed class SeaManagementAgentCapability(SeaManagementClinicToolAdapter tool) : AssistantCapability([tool], [
    AssistantActionPolicies.Prepare(SeaManagementAssistantActionCommitter.SeaManagementActionType, "sea-management", [],
        requiredRoles: [AssistantOperationsScope.TenantOwnerRole], toolNames: [Names.ManageCareTeamAccess])
])
{
    public override string Name => ProcedureCapabilityNames.CareTeamAccess;
    public override string Description => "Manage care-team access roles (who may read or change which steps) and read practice statistics.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) => context.SubjectKind == "practice" && AssistantOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() =>
        $"Use {Names.ManageCareTeamAccess} for access roles, their members, which roles may read or change a step, and practice statistics. Changes are proposals the user confirms.";
}

public sealed class SeaManagementClinicToolAdapter(ISeaManagementAgentTool inner) : IAssistantPreparationTool
{
    /// <summary>The SEA operations this adapter offers; anything else is refused before it reaches SEA.</summary>
    public static IReadOnlyList<string> Operations { get; } =
    [
        SeaManagementOperations.ListRoles, SeaManagementOperations.CreateRole, SeaManagementOperations.UpdateRole, SeaManagementOperations.DeleteRole,
        SeaManagementOperations.ListUserRoles, SeaManagementOperations.AssignUserRole, SeaManagementOperations.RemoveUserRole,
        SeaManagementOperations.ListEventRoles, SeaManagementOperations.GetEventReadRoles, SeaManagementOperations.GetEventWriteRoles,
        SeaManagementOperations.AssignEventRole, SeaManagementOperations.RemoveEventRole, SeaManagementOperations.GetStatistics
    ];

    public AgentToolDefinition Definition { get; } = new(
        Names.ManageCareTeamAccess,
        "Read or prepare changes to care-team access roles: list, create, rename or delete a role; list, assign or remove a staff member's roles; " +
        "list, assign or remove the roles that may read or change a step (payload id is the step id); and read practice statistics. Changes require confirmation.",
        "{\"type\":\"object\",\"properties\":{\"operation\":{\"type\":\"string\",\"enum\":[" +
        string.Join(",", Operations.Select(operation => $"\"{operation}\"")) +
        "]},\"payload\":{\"type\":\"object\"}},\"required\":[\"operation\"],\"additionalProperties\":false}");

    public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using (JsonDocument document = JsonDocument.Parse(call.ArgumentsJson))
        {
            string? operation = document.RootElement.TryGetProperty(SeaManagementOperations.Operation, out JsonElement value) ? value.GetString() : null;
            if (operation is null || !Operations.Contains(operation, StringComparer.Ordinal))
                throw new JsonException("This operation is not offered here; use the procedure and step tools for patients' records.");
        }

        return inner.ExecuteAsync(call, context, cancellationToken);
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        SeaManagementPreparedAction? action = inner.ParsePreparedAction(result);
        return action is null
            ? null
            : new(ProcedureCapabilityNames.CareTeamAccess, SeaManagementAssistantActionCommitter.SeaManagementActionType, action.ActionId, JsonSerializer.Serialize(action), action.ExpiresAt);
    }
}
