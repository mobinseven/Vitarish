using Vitarish.Authentication.Domain.Methods;

namespace Vitarish.Authentication.Domain.Security;

public sealed record SecurityAccessPolicy(
    PasswordPolicy PasswordPolicy,
    int FailedAttemptThreshold,
    TimeSpan FailedAttemptDenialDuration,
    FailedAttemptResponse FailedAttemptResponse,
    IReadOnlySet<AuthenticationMethod> EnabledAuthenticationMechanisms,
    IReadOnlySet<PreAuthenticationAction> PermittedPreAuthenticationActions,
    int MaximumConcurrentSessions = 1,
    bool ConcurrentSessionsFunctionallyRequired = false,
    int DefaultInactivityTimeoutMinutes = 30,
    IReadOnlySet<string>? AllowedSessionStartCidrs = null)
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
        if (MaximumConcurrentSessions is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(MaximumConcurrentSessions), "The concurrent-session maximum must be between 1 and 3.");
        if (MaximumConcurrentSessions > 1 && !ConcurrentSessionsFunctionallyRequired)
            throw new ArgumentException("Concurrent sessions require a recorded functional need.", nameof(ConcurrentSessionsFunctionallyRequired));
        if (DefaultInactivityTimeoutMinutes is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(DefaultInactivityTimeoutMinutes), "The default inactivity timeout must be between 1 and 30 minutes.");
        if (AllowedSessionStartCidrs is not null)
        {
            foreach (string cidr in AllowedSessionStartCidrs)
            {
                if (!SessionStartCidr.TryParse(cidr, out string? normalized) || !string.Equals(cidr, normalized, StringComparison.Ordinal))
                    throw new ArgumentException("Allowed session-start values must be normalized CIDRs.", nameof(AllowedSessionStartCidrs));
            }
        }
    }

    public bool Allows(AuthenticationMethod mechanism, PreAuthenticationAction action) =>
        EnabledAuthenticationMechanisms.Contains(mechanism) && PermittedPreAuthenticationActions.Contains(action);

    public bool EquivalentTo(SecurityAccessPolicy other) =>
        PasswordPolicy == other.PasswordPolicy &&
        FailedAttemptThreshold == other.FailedAttemptThreshold &&
        FailedAttemptDenialDuration == other.FailedAttemptDenialDuration &&
        FailedAttemptResponse == other.FailedAttemptResponse &&
        MaximumConcurrentSessions == other.MaximumConcurrentSessions &&
        ConcurrentSessionsFunctionallyRequired == other.ConcurrentSessionsFunctionallyRequired &&
        DefaultInactivityTimeoutMinutes == other.DefaultInactivityTimeoutMinutes &&
        EnabledAuthenticationMechanisms.SetEquals(other.EnabledAuthenticationMechanisms) &&
        PermittedPreAuthenticationActions.SetEquals(other.PermittedPreAuthenticationActions) &&
        (AllowedSessionStartCidrs ?? new HashSet<string>(StringComparer.Ordinal)).SetEquals(other.AllowedSessionStartCidrs ?? new HashSet<string>(StringComparer.Ordinal));

    public SecurityAccessPolicy ReconcileProfile(AuthenticationProfilePolicy profilePolicy)
    {
        HashSet<AuthenticationMethod> mechanisms = EnabledAuthenticationMechanisms
            .Where(profilePolicy.EnabledAuthenticationMechanisms.Contains)
            .ToHashSet();
        mechanisms.UnionWith(profilePolicy.RequiredAuthenticationMechanisms);

        HashSet<PreAuthenticationAction> actions = PermittedPreAuthenticationActions
            .Where(profilePolicy.PermittedPreAuthenticationActions.Contains)
            .ToHashSet();
        actions.UnionWith(profilePolicy.RequiredPreAuthenticationActions);

        return this with
        {
            EnabledAuthenticationMechanisms = mechanisms,
            PermittedPreAuthenticationActions = actions,
            AllowedSessionStartCidrs = AllowedSessionStartCidrs ?? new HashSet<string>(StringComparer.Ordinal)
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
