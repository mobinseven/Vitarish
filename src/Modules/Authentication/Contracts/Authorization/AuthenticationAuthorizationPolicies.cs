namespace Vitarish.Authentication.Contracts.Authorization;

public static class AuthenticationAuthorizationPolicies
{
    public const string ReadSessionPolicy = "Authentication.SecuritySessionPolicy.Read";
    public const string ChangeSessionPolicy = "Authentication.SecuritySessionPolicy.Change";
    public const string ReadAuthenticationPolicy = "Authentication.SecurityAuthenticationPolicy.Read";
    public const string ChangeAuthenticationPolicy = "Authentication.SecurityAuthenticationPolicy.Change";
    public const string ChangeUserInactivityPolicy = "Authentication.UserInactivityPolicy.Change";
}
