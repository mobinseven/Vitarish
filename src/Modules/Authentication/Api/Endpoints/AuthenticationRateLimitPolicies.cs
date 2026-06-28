namespace Vitarish.Authentication.Api.Endpoints;

public static class AuthenticationRateLimitPolicies
{
    public const string PasswordLogin = "authentication-password-login";
    public const string OtpBegin = "authentication-otp-begin";
    public const string OtpComplete = "authentication-otp-complete";
    public const string Passkey = "authentication-passkey";
}
