using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Contracts.Responses;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>
/// Commits a confirmed staff removal in the confirming request's context. It first checks that the member is
/// still the one the owner saw, then sends the Users removal command, whose own authorization, owner protection,
/// active-session rule and audit apply.
/// </summary>
public sealed class StaffRemovalAssistantActionCommitter(ISender sender) : IAssistantActionCommitter
{
    public string ActionType => AssistantStaffManagement.RemoveActionType;
    public string WorkflowKey => AssistantStaffManagement.RemoveWorkflowKey;

    public async Task<AssistantActionCommitResult> CommitAsync(
        AssistantAction action,
        AssistantContext context,
        CancellationToken cancellationToken)
    {
        StaffRemovalReview? review;
        try
        {
            review = JsonSerializer.Deserialize(action.ReviewPayload, StaffManagementJsonSerializerContext.Default.StaffRemovalReview);
        }
        catch (JsonException)
        {
            review = null;
        }

        if (review is null || string.IsNullOrWhiteSpace(review.UserId))
            return AssistantActionCommitResult.Failed(AssistantStaffManagement.FailureCodes.InvalidReview, "The staff removal review is invalid.");

        TenantMemberSummary? member = (await sender.Send(new ListTenantMembersQuery(null, 1, [review.UserId]), cancellationToken)).SingleOrDefault();
        if (member is null || StaffMemberView.DisplayName(member) != review.Name)
            return AssistantActionCommitResult.Failed(AssistantStaffManagement.FailureCodes.MemberChanged, "This member changed or left the practice after the review was prepared.");

        IdentityResult result = await sender.Send(new RemoveTenantMemberCommand(review.UserId), cancellationToken);
        if (result.Succeeded)
            return AssistantActionCommitResult.Completed(JsonSerializer.Serialize(
                new StaffRemovalCompletion(review.UserId), StaffManagementJsonSerializerContext.Default.StaffRemovalCompletion));

        IdentityError? error = result.Errors.FirstOrDefault();
        return error?.Code switch
        {
            UsersCommandErrorCodes.Unauthorized => AssistantActionCommitResult.Failed(
                AssistantStaffManagement.FailureCodes.PermissionDenied, UsersCommandErrorCodes.UnauthorizedDescription),
            UsersCommandErrorCodes.TenantOwnerRemovalRejected => AssistantActionCommitResult.Failed(
                AssistantStaffManagement.FailureCodes.OwnerRemovalRejected, UsersCommandErrorCodes.TenantOwnerRemovalRejectedDescription),
            UsersCommandErrorCodes.ActiveSessionConflict => AssistantActionCommitResult.Failed(
                AssistantStaffManagement.FailureCodes.ActiveSessionConflict, UsersCommandErrorCodes.ActiveSessionConflictDescription, retryable: true),
            UsersCommandErrorCodes.TenantMemberConflict => AssistantActionCommitResult.Failed(
                AssistantStaffManagement.FailureCodes.MembershipConflict, UsersCommandErrorCodes.TenantMemberConflictDescription, retryable: true),
            _ => AssistantActionCommitResult.Failed(
                AssistantStaffManagement.FailureCodes.RemovalFailed, "The staff member could not be removed.")
        };
    }
}
