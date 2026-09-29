using Vitarish.Authentication.Domain.Methods;

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

    public IReadOnlySet<AuthenticationMethod> EnabledAuthenticationMechanisms
    {
        get
        {
            HashSet<AuthenticationMethod> mechanisms = [AuthenticationMethod.Password];
            if (AllowPasswordlessPhoneLogin || UsePhoneOtpForTwoFactorAccounts)
                mechanisms.Add(AuthenticationMethod.Phone);
            if (AllowPasswordlessEmailLogin)
                mechanisms.Add(AuthenticationMethod.Email);
            if (AllowPasskeyLogin)
                mechanisms.Add(AuthenticationMethod.Passkey);
            return mechanisms;
        }
    }

    public IReadOnlySet<PreAuthenticationAction> PermittedPreAuthenticationActions
    {
        get
        {
            HashSet<PreAuthenticationAction> actions =
            [
                PreAuthenticationAction.AuthenticationProfile,
                PreAuthenticationAction.PasswordLogin
            ];
            if (AllowPasswordlessPhoneLogin)
                actions.Add(PreAuthenticationAction.PhoneLogin);
            if (AllowPasswordlessEmailLogin)
                actions.Add(PreAuthenticationAction.EmailLogin);
            if (AllowPasskeyLogin)
                actions.Add(PreAuthenticationAction.PasskeyLogin);
            if (UsePhoneOtpForTwoFactorAccounts)
                actions.Add(PreAuthenticationAction.PasswordPlusPhoneOtp);
            return actions;
        }
    }

    public IReadOnlySet<AuthenticationMethod> RequiredAuthenticationMechanisms
    {
        get
        {
            HashSet<AuthenticationMethod> required = [AuthenticationMethod.Password];
            if (UsePhoneOtpForTwoFactorAccounts)
                required.Add(AuthenticationMethod.Phone);
            return required;
        }
    }

    public IReadOnlySet<PreAuthenticationAction> RequiredPreAuthenticationActions
    {
        get
        {
            HashSet<PreAuthenticationAction> required =
            [
                PreAuthenticationAction.AuthenticationProfile,
                PreAuthenticationAction.PasswordLogin
            ];
            if (UsePhoneOtpForTwoFactorAccounts)
                required.Add(PreAuthenticationAction.PasswordPlusPhoneOtp);
            return required;
        }
    }
}
