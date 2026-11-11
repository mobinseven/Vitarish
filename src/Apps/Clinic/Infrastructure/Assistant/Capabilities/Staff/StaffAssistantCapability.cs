using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Staff;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>Lets a practice owner have the assistant prepare a staff invitation for confirmation.</summary>
public sealed class StaffAssistantCapability(PrepareInviteMemberTool tool) : AssistantCapability([tool], [
    AssistantActionPolicies.Prepare(
        AssistantStaffInvitation.ActionType,
        AssistantStaffInvitation.WorkflowKey,
        [],
        [AssistantOperationsScope.TenantOwnerRole],
        [AssistantStaffInvitation.PrepareToolName],
        retryClassification: AssistantActionRetryClassification.Retryable)
])
{
    public override string Name => AssistantStaffInvitation.CapabilityKey;
    public override string Description => "Prepare invitations for new staff members of the authenticated practice.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);

    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == AssistantSubjects.PracticeKind &&
        AssistantOperationsScope.IsAuthorizedRole(context.Role);

    public override string GetInstructions() =>
        $"Use {AssistantStaffInvitation.PrepareToolName} with exactly one of phoneNumber or email plus firstName and lastName. " +
        "The invited person always becomes staff; never ask for or choose a role. The invitation is only prepared for the owner's " +
        "review and is sent when the owner confirms; never claim it was sent during the run.";
}
