using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Responses;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>
/// Commits a confirmed staff invitation by sending the Users provisioning command in the confirming request's
/// context, so the command's own authorization re-checks the caller. The command writes the account audit.
/// </summary>
public sealed class StaffInvitationAssistantActionCommitter(
    ISender sender,
    ILogger<StaffInvitationAssistantActionCommitter>? logger = null) : IAssistantActionCommitter
{
    private const string CompletedOutcome = "completed";
    private const string AlreadyMemberOutcome = "already_member";

    public string ActionType => AssistantStaffInvitation.ActionType;
    public string WorkflowKey => AssistantStaffInvitation.WorkflowKey;

    public async Task<AssistantActionCommitResult> CommitAsync(
        AssistantAction action,
        AssistantContext context,
        CancellationToken cancellationToken)
    {
        StaffInvitationReview? review;
        try
        {
            review = JsonSerializer.Deserialize(action.ReviewPayload, StaffInvitationJsonSerializerContext.Default.StaffInvitationReview);
        }
        catch (JsonException)
        {
            review = null;
        }

        if (review is null)
            return Failed(action, null, AssistantStaffInvitation.FailureCodes.InvalidReview, "The staff invitation review is invalid.");

        string masked = AssistantContactMasking.Contact(review.PhoneNumber, review.Email);
        ProvisionTenantMemberResult result = await sender.Send(
            new ProvisionTenantMemberCommand(null, review.Email, review.PhoneNumber, review.FirstName, review.LastName),
            cancellationToken);

        if (result.User is not null && (result.Created || result.AlreadyMember))
        {
            Log(action, masked, result.AlreadyMember ? AlreadyMemberOutcome : CompletedOutcome);
            return AssistantActionCommitResult.Completed(JsonSerializer.Serialize(
                new StaffInvitationCompletion(result.User.Id, result.Created, result.AlreadyMember),
                StaffInvitationJsonSerializerContext.Default.StaffInvitationCompletion));
        }

        return result.ErrorCode switch
        {
            UsersCommandErrorCodes.Unauthorized => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.PermissionDenied, UsersCommandErrorCodes.UnauthorizedDescription),
            UsersCommandErrorCodes.InvalidContact => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.InvalidContact, UsersCommandErrorCodes.InvalidContactDescription),
            UsersCommandErrorCodes.ActiveSessionConflict => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.ActiveSessionConflict, UsersCommandErrorCodes.ActiveSessionConflictDescription, retryable: true),
            UsersCommandErrorCodes.ContactIdentifierInUse => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.ContactInUse, UsersCommandErrorCodes.ContactIdentifierInUseDescription),
            UsersCommandErrorCodes.RoleScopeConflict => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.RoleScopeConflict, UsersCommandErrorCodes.RoleScopeConflictDescription),
            UsersCommandErrorCodes.TenantMemberConflict => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.MembershipConflict, UsersCommandErrorCodes.TenantMemberConflictDescription, retryable: true),
            _ => Failed(action, masked,
                AssistantStaffInvitation.FailureCodes.InvitationFailed, "The staff invitation could not be completed.")
        };
    }

    private AssistantActionCommitResult Failed(
        AssistantAction action, string? masked, string code, string message, bool retryable = false)
    {
        if (masked is not null)
            Log(action, masked, code);
        return AssistantActionCommitResult.Failed(code, message, retryable);
    }

    private void Log(AssistantAction action, string masked, string outcome)
    {
        if (logger is not null)
            StaffInvitationTelemetry.Committed(logger, masked, outcome, action.ActionId);
    }
}
