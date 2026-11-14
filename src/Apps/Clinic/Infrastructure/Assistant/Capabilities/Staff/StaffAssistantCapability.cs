using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Staff;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>
/// Lets a practice owner have the assistant list the practice's members and prepare a staff invitation or a staff
/// removal for confirmation. Role changes are not offered: only a platform administrator may change a role.
/// </summary>
public sealed class StaffAssistantCapability(
    PrepareInviteMemberTool inviteTool,
    ListStaffMembersTool listTool,
    PrepareRemoveMemberTool removeTool) : AssistantCapability([inviteTool, listTool, removeTool], [
    AssistantActionPolicies.Prepare(
        AssistantStaffInvitation.ActionType,
        AssistantStaffInvitation.WorkflowKey,
        [],
        [AssistantOperationsScope.TenantOwnerRole],
        [AssistantStaffInvitation.PrepareToolName],
        retryClassification: AssistantActionRetryClassification.Retryable,
        // The review holds the tool's own arguments, so an owner's edit re-runs the same validation.
        revisable: true),
    AssistantActionPolicies.Prepare(
        AssistantStaffManagement.RemoveActionType,
        AssistantStaffManagement.RemoveWorkflowKey,
        [],
        [AssistantOperationsScope.TenantOwnerRole],
        [AssistantStaffManagement.PrepareRemoveToolName],
        retryClassification: AssistantActionRetryClassification.Retryable)
])
{
    public override string Name => AssistantStaffInvitation.CapabilityKey;
    public override string Description => "List the practice's members and prepare staff invitations and removals.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);

    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == AssistantSubjects.PracticeKind &&
        AssistantOperationsScope.IsAuthorizedRole(context.Role);

    public override string GetInstructions() =>
        $"Use {AssistantStaffInvitation.PrepareToolName} with exactly one of phoneNumber or email plus firstName and lastName. " +
        "The invited person always becomes staff; never ask for or choose a role. " +
        $"Use {AssistantStaffManagement.ListToolName} to find members, and {AssistantStaffManagement.PrepareRemoveToolName} with a member's userId to prepare removing a staff member; owners cannot be removed. " +
        "Roles cannot be changed here; say that a platform administrator changes roles. " +
        "Invitations and removals are only prepared for the owner's review and happen when the owner confirms; never claim one happened during the run.";
}
