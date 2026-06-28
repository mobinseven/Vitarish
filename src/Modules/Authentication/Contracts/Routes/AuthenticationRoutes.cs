namespace Vitarish.Authentication.Contracts.Routes;

public static class AuthenticationRoutes
{
    public const string BasePath = "/api/modules/authentication";
    public const string Session = BasePath + "/session";
    public const string Antiforgery = BasePath + "/antiforgery";
    public const string PasswordLogin = BasePath + "/login/password";
    public const string BeginPhoneLogin = BasePath + "/login/phone/begin";
    public const string CompletePhoneLogin = BasePath + "/login/phone/complete";
    public const string BeginEmailLogin = BasePath + "/login/email/begin";
    public const string CompleteEmailLogin = BasePath + "/login/email/complete";
    public const string BeginPasskeyLogin = BasePath + "/login/passkey/begin";
    public const string CompletePasskeyLogin = BasePath + "/login/passkey/complete";
    public const string LogoutContext = BasePath + "/logout/context";
    public const string Logout = BasePath + "/logout";
    public const string Me = BasePath + "/me";
    public const string BeginChangePhone = BasePath + "/account/phone/begin";
    public const string CompleteChangePhone = BasePath + "/account/phone/complete";
    public const string BeginChangeEmail = BasePath + "/account/email/begin";
    public const string CompleteChangeEmail = BasePath + "/account/email/complete";
    public const string ChangePassword = BasePath + "/account/password";
}
