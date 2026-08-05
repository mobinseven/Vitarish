namespace Vitarish.Users.Contracts.Routes;

public static class UsersApiRoutes
{
    public const string Base = "/api/modules/users";
    public const string Search = "/search";
    public const string ById = "/{userId}";
    public const string CheckEmail = "/email-in-use";
    public const string CheckPhone = "/phone-in-use";
    public const string CheckRole = "/role-membership";
    public const string CreateWithPhone = "/create/phone";
    public const string CreateWithEmail = "/create/email";
    public const string SetEmail = "/{userId}/email";
    public const string AddRole = "/{userId}/roles";
    public const string Administration = "/administration";
    public const string AdministrationCreateWithPhone = "/administration/create/phone";
    public const string AdministrationCreateWithEmail = "/administration/create/email";
    public const string AdministrationUserRoles = "/{userId}/tenant-roles";
    public const string AdministrationUserRole = "/{userId}/tenant-roles/{tenantId:guid}/{roleSuffix}";
    public const string Profile = "/{userId}/profile";
    public const string Culture = "/{userId}/culture";
    public const string WebAuthnCredentials = "/{userId}/webauthn";
    public const string WebAuthnBeginRegister = "/{userId}/webauthn/begin-register";
    public const string WebAuthnCompleteRegister = "/{userId}/webauthn/complete-register";
    public const string WebAuthnDelete = "/{userId}/webauthn/{credentialId}";
    public const string PatBase = "/{userId}/tokens";
    public const string PatRevoke = "/{userId}/tokens/{token}";
}
