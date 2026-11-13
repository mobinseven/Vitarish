namespace Vitarish.Clinic.Application.Assistant.Staff;

/// <summary>The one owner of the names the staff-invitation capability, its action, tool and workflow share.</summary>
public static class AssistantStaffInvitation
{
    public const string CapabilityKey = "Staff";
    public const string ActionType = "invite_member";
    public const string WorkflowKey = "staff-invitations";
    public const string PrepareToolName = "prepare_invite_member";

    public static class FailureCodes
    {
        public const string InvalidReview = "invalid_staff_invitation";
        public const string InvalidContact = "invalid_contact";
        public const string PermissionDenied = "permission_denied";
        public const string ActiveSessionConflict = "active_session_conflict";
        public const string ContactInUse = "contact_in_use";
        public const string RoleScopeConflict = "role_scope_conflict";
        public const string MembershipConflict = "membership_conflict";
        public const string InvitationFailed = "staff_invitation_failed";
    }
}
