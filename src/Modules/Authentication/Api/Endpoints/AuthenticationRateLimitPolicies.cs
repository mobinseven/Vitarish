namespace Vitarish.Authentication.Api.Endpoints;

public static class AuthenticationRateLimitPolicies
{
    public const string Continuation = "authentication-continuation";
    public const int ContinuationPermitLimit = 120;
    public const string PasswordLogin = "authentication-password-login";
    public const string OtpBegin = "authentication-otp-begin";
    public const string OtpComplete = "authentication-otp-complete";
    public const string Passkey = "authentication-passkey";
    public const string AccountSecurityMutation = "authentication-account-security-mutation";
}
