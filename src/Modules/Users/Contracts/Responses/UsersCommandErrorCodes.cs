namespace Vitarish.Users.Contracts.Responses;

public static class UsersCommandErrorCodes
{
    public const string Unauthorized = "Unauthorized";
    public const string UnauthorizedDescription = "The caller is not authorized to manage this user security state.";
    public const string ActiveSessionConflict = "ActiveSessionConflict";
    public const string ActiveSessionConflictDescription = "The mutation cannot be applied while the user has an active session.";
    public const string RoleNotFound = "RoleNotFound";
    public const string RoleNotFoundDescription = "The requested global role does not exist.";
    public const string InvalidGlobalRole = "InvalidGlobalRole";
    public const string InvalidGlobalRoleDescription = "Only the explicitly permitted global roles may use generic role assignment.";
    public const string InvalidTenantRole = "InvalidTenantRole";
    public const string InvalidTenantRoleDescription = "The tenant role is not permitted.";
    public const string UserNotFound = "UserNotFound";
    public const string UserNotFoundDescription = "The user was not found.";
    public const string TenantRoleConflict = "TenantRoleConflict";
    public const string TenantRoleConflictDescription = "The tenant role changed concurrently; retry the operation.";
}
