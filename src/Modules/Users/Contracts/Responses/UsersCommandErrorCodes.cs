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
    public const string InvalidContact = "InvalidContact";
    public const string InvalidContactDescription = "At least one valid email address or E.164 phone number is required.";
    public const string ConcurrencyFailure = "ConcurrencyFailure";
    public const string ConcurrencyFailureDescription = "The user changed concurrently; reload the account and retry the edit.";
    public const string TenantRoleConflict = "TenantRoleConflict";
    public const string TenantRoleConflictDescription = "The tenant role changed concurrently; retry the operation.";
    public const string TenantMemberNotFound = "TenantMemberNotFound";
    public const string TenantMemberNotFoundDescription = "The requested tenant member was not found.";
    public const string TenantOwnerRemovalRejected = "TenantOwnerRemovalRejected";
    public const string TenantOwnerRemovalRejectedDescription = "The tenant owner cannot be removed from the tenant staff surface.";
    public const string TenantMemberConflict = "TenantMemberConflict";
    public const string TenantMemberConflictDescription = "The tenant membership changed concurrently; retry the operation.";
    public const string LastOwnerProtection = "LastOwnerProtection";
    public const string LastOwnerProtectionDescription = "The last tenant owner cannot be removed or demoted.";
}
