namespace Vitarish.Clinic.Application.Assistant.Staff;

/// <summary>The one owner of the names the staff list and staff-removal tools, action and workflow share.</summary>
public static class AssistantStaffManagement
{
    public const string ListToolName = "list_staff_members";
    public const string PrepareRemoveToolName = "prepare_remove_member";
    public const string RemoveActionType = "remove_member";
    public const string RemoveWorkflowKey = "staff-removals";

    public static class FailureCodes
    {
        public const string InvalidReview = "invalid_staff_removal";
        public const string MemberChanged = "member_changed";
        public const string PermissionDenied = "permission_denied";
        public const string OwnerRemovalRejected = "owner_removal_rejected";
        public const string ActiveSessionConflict = "active_session_conflict";
        public const string MembershipConflict = "membership_conflict";
        public const string RemovalFailed = "staff_removal_failed";
    }
}
