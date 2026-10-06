namespace Vitarish.Users.Contracts.Routes;

public static class UsersApiRoutes
{
    public const string Base = "/api/modules/users";
    public const string UserIdRouteValue = "userId";
    public const string Search = "/search";
    public const string ById = $"/{{{UserIdRouteValue}}}";
    public const string CheckEmail = "/email-in-use";
    public const string CheckPhone = "/phone-in-use";
    public const string CheckRole = "/role-membership";
    public const string CreateWithPhone = "/create/phone";
    public const string CreateWithEmail = "/create/email";
    public const string SetEmail = $"/{{{UserIdRouteValue}}}/email";
    public const string DisableUser = $"/{{{UserIdRouteValue}}}/disable";
    public const string EnableUser = $"/{{{UserIdRouteValue}}}/enable";
    public const string AddRole = $"/{{{UserIdRouteValue}}}/roles";
    public const string Administration = "/administration";
    public const string AdministrationCreateWithPhone = "/administration/create/phone";
    public const string AdministrationCreateWithEmail = "/administration/create/email";
    public const string CreateWithPhoneOperation = "UsersCreateWithPhone";
    public const string CreateWithEmailOperation = "UsersCreateWithEmail";
    public const string SetEmailOperation = "UsersSetEmail";
    public const string DisableUserOperation = "UsersDisableUser";
    public const string EnableUserOperation = "UsersEnableUser";
    public const string AdministrationCreateWithPhoneOperation = "UsersAdministrationCreateWithPhone";
    public const string AdministrationCreateWithEmailOperation = "UsersAdministrationCreateWithEmail";
    public const string AdministrationUpdateUserOperation = "UsersAdministrationUpdateUser";
    public const string AdministrationUserSecurity = $"/{{{UserIdRouteValue}}}/security";
    public const string AdministrationGetUserSecurityOperation = "UsersAdministrationGetUserSecurity";
    public const string AdministrationUserRoles = $"/{{{UserIdRouteValue}}}/tenant-roles";
    public const string AdministrationUserRole = $"/{{{UserIdRouteValue}}}/tenant-roles/{{tenantId:guid}}/{{roleSuffix}}";
    public const string TenantMembers = "/tenant-members";
    public const string TenantMember = "/tenant-members/{userId}";

    public const string ListTenantMembersOperation = "UsersListTenantMembers";
    public const string ProvisionTenantMemberOperation = "UsersProvisionTenantMember";
    public const string RemoveTenantMemberOperation = "UsersRemoveTenantMember";
    public const string Profile = $"/{{{UserIdRouteValue}}}/profile";
    public const string Culture = $"/{{{UserIdRouteValue}}}/culture";
    public const string WebAuthnCredentials = $"/{{{UserIdRouteValue}}}/webauthn";
    public const string WebAuthnBeginRegister = $"/{{{UserIdRouteValue}}}/webauthn/begin-register";
    public const string WebAuthnCompleteRegister = $"/{{{UserIdRouteValue}}}/webauthn/complete-register";
    public const string WebAuthnDelete = $"/{{{UserIdRouteValue}}}/webauthn/{{credentialId}}";
    public const string PatBase = $"/{{{UserIdRouteValue}}}/tokens";
    public const string PatRevoke = $"/{{{UserIdRouteValue}}}/tokens/{{token}}";
}
