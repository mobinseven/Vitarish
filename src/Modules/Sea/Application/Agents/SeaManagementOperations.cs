namespace Vitarish.Sea.Application.Agents;

/// <summary>Names of the SEA management operations that other boundaries prepare through <see cref="ISeaManagementAgentTool"/>.</summary>
public static class SeaManagementOperations
{
    public const string CreateProcedure = "create_procedure";
    public const string CommitEvent = "commit_event";
    public const string UncommitEvent = "uncommit_event";
    public const string CreateAttachment = "create_attachment";
    public const string CreateReminder = "create_reminder";
    public const string ListRoles = "list_roles";
    public const string CreateRole = "create_role";
    public const string UpdateRole = "update_role";
    public const string DeleteRole = "delete_role";
    public const string ListUserRoles = "list_user_roles";
    public const string AssignUserRole = "assign_user_role";
    public const string RemoveUserRole = "remove_user_role";
    public const string ListEventRoles = "list_event_roles";
    public const string GetEventReadRoles = "get_event_read_roles";
    public const string GetEventWriteRoles = "get_event_write_roles";
    public const string AssignEventRole = "assign_event_role";
    public const string RemoveEventRole = "remove_event_role";
    public const string GetStatistics = "get_statistics";
    public const string Operation = "operation";
    public const string Payload = "payload";
}
