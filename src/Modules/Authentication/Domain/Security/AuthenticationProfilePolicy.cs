namespace Vitarish.Authentication.Domain.Security;

public sealed record AuthenticationProfilePolicy(
    bool UsePhoneOtpForTwoFactorAccounts,
    bool AllowPasswordlessPhoneLogin,
    bool AllowPasswordlessEmailLogin,
    bool AllowPasskeyLogin)
{
    public static AuthenticationProfilePolicy Default { get; } = new(
        UsePhoneOtpForTwoFactorAccounts: false,
        AllowPasswordlessPhoneLogin: true,
        AllowPasswordlessEmailLogin: true,
        AllowPasskeyLogin: true);

    public static AuthenticationProfilePolicy ProductEvaluated { get; } = new(
        UsePhoneOtpForTwoFactorAccounts: true,
        AllowPasswordlessPhoneLogin: false,
        AllowPasswordlessEmailLogin: false,
        AllowPasskeyLogin: false);

    public static AuthenticationProfilePolicy OpenApi { get; } = new(
        UsePhoneOtpForTwoFactorAccounts: true,
        AllowPasswordlessPhoneLogin: true,
        AllowPasswordlessEmailLogin: true,
        AllowPasskeyLogin: true);
}
