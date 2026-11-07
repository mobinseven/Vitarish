namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string BasePath = "/api/modules/authentication";
    public const string Session = BasePath + "/session";
    public const string Antiforgery = BasePath + "/antiforgery";
    public const string ProfilePath = "/profile";
    public const string Profile = BasePath + ProfilePath;
    public const string PasswordLoginPath = "/login/password";
    public const string PasswordLogin = BasePath + PasswordLoginPath;
    public const string PasswordLoginOtpCompletePath = "/login/password/otp/complete";
    public const string PasswordLoginOtpResendPath = "/login/password/otp/resend";
    public const string PasswordLoginOtpComplete = BasePath + PasswordLoginOtpCompletePath;
    public const string PasswordLoginOtpResend = BasePath + PasswordLoginOtpResendPath;
    public const string BeginPhoneLoginPath = "/login/phone/begin";
    public const string BeginPhoneLogin = BasePath + BeginPhoneLoginPath;
    public const string CompletePhoneLogin = BasePath + "/login/phone/complete";
    public const string BeginEmailLoginPath = "/login/email/begin";
    public const string BeginEmailLogin = BasePath + BeginEmailLoginPath;
    public const string CompleteEmailLogin = BasePath + "/login/email/complete";
    public const string BeginPasswordRecoveryPath = "/password-recovery/begin";
    public const string CompletePasswordRecoveryPath = "/password-recovery/complete";
    public const string BeginPasswordRecovery = BasePath + BeginPasswordRecoveryPath;
    public const string CompletePasswordRecovery = BasePath + CompletePasswordRecoveryPath;
    public const string BeginPasskeyLogin = BasePath + "/login/passkey/begin";
    public const string CompletePasskeyLogin = BasePath + "/login/passkey/complete";
    public const string LogoutContext = BasePath + "/logout/context";
    public const string Logout = BasePath + "/logout";
    public const string Me = BasePath + "/me";
    public const string BeginChangePhone = BasePath + "/account/phone/begin";
    public const string CompleteChangePhone = BasePath + "/account/phone/complete";
    public const string BeginChangeEmail = BasePath + "/account/email/begin";
    public const string CompleteChangeEmail = BasePath + "/account/email/complete";
    public const string ChangePasswordPath = "/account/password";
    public const string ChangePassword = BasePath + ChangePasswordPath;
    public const string SetFirstPasswordPath = "/account/password/first";
    public const string SetFirstPassword = BasePath + SetFirstPasswordPath;
    public const string TwoStepSignInPath = "/account/two-step-sign-in";
    public const string TwoStepSignIn = BasePath + TwoStepSignInPath;
}
