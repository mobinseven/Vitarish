using Vitarish.Authentication.Domain.Methods;

namespace Vitarish.Authentication.Domain.Security;

public sealed record SecurityAccessPolicy(
    PasswordPolicy PasswordPolicy,
    int FailedAttemptThreshold,
    TimeSpan FailedAttemptDenialDuration,
    FailedAttemptResponse FailedAttemptResponse,
    IReadOnlySet<AuthenticationMethod> EnabledAuthenticationMechanisms,
    IReadOnlySet<PreAuthenticationAction> PermittedPreAuthenticationActions)
{
    public static SecurityAccessPolicy Default { get; } = ForProfile(AuthenticationProfilePolicy.Default);

    public static SecurityAccessPolicy ProductEvaluated { get; } = ForProfile(AuthenticationProfilePolicy.ProductEvaluated);

    public static SecurityAccessPolicy ForProfile(AuthenticationProfilePolicy profilePolicy) => new(
        PasswordPolicy.Default,
        FailedAttemptThreshold: 5,
        FailedAttemptDenialDuration: TimeSpan.FromMinutes(15),
        FailedAttemptResponse.Lockout,
        profilePolicy.EnabledAuthenticationMechanisms,
        profilePolicy.PermittedPreAuthenticationActions);

    public void ValidateConfiguration()
    {
        PasswordPolicy.ValidateConfiguration();
        if (FailedAttemptThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(FailedAttemptThreshold), "Failed-attempt threshold must be positive.");
        if (FailedAttemptDenialDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(FailedAttemptDenialDuration), "Failed-attempt denial duration must be positive.");
        if (!EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Password))
            throw new ArgumentException("Username/password authentication must remain enabled.", nameof(EnabledAuthenticationMechanisms));
    }

    public bool Allows(AuthenticationMethod mechanism, PreAuthenticationAction action) =>
        EnabledAuthenticationMechanisms.Contains(mechanism) && PermittedPreAuthenticationActions.Contains(action);

    public bool EquivalentTo(SecurityAccessPolicy other) =>
        PasswordPolicy == other.PasswordPolicy &&
        FailedAttemptThreshold == other.FailedAttemptThreshold &&
        FailedAttemptDenialDuration == other.FailedAttemptDenialDuration &&
        FailedAttemptResponse == other.FailedAttemptResponse &&
        EnabledAuthenticationMechanisms.SetEquals(other.EnabledAuthenticationMechanisms) &&
        PermittedPreAuthenticationActions.SetEquals(other.PermittedPreAuthenticationActions);

    public SecurityAccessPolicy ReconcileProfile(AuthenticationProfilePolicy profilePolicy)
    {
        HashSet<AuthenticationMethod> mechanisms = EnabledAuthenticationMechanisms
            .Where(profilePolicy.EnabledAuthenticationMechanisms.Contains)
            .ToHashSet();
        _ = mechanisms.Add(AuthenticationMethod.Password);
        if (profilePolicy.UsePhoneOtpForTwoFactorAccounts)
            _ = mechanisms.Add(AuthenticationMethod.Phone);

        HashSet<PreAuthenticationAction> actions = PermittedPreAuthenticationActions
            .Where(profilePolicy.PermittedPreAuthenticationActions.Contains)
            .ToHashSet();
        _ = actions.Add(PreAuthenticationAction.AuthenticationProfile);
        _ = actions.Add(PreAuthenticationAction.PasswordLogin);
        if (profilePolicy.UsePhoneOtpForTwoFactorAccounts)
            _ = actions.Add(PreAuthenticationAction.PasswordPlusPhoneOtp);

        return this with
        {
            EnabledAuthenticationMechanisms = mechanisms,
            PermittedPreAuthenticationActions = actions
        };
    }
}

public enum FailedAttemptResponse
{
    Deny,
    Lockout
}

public enum PreAuthenticationAction
{
    AuthenticationProfile,
    PasswordLogin,
    PhoneLogin,
    EmailLogin,
    PasskeyLogin,
    PasswordPlusPhoneOtp
}
