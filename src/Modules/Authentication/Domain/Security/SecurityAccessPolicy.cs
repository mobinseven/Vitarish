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
    int DefaultInactivityTimeoutMinutes = SessionInactivityTimeout.DefaultMinutes,
    IReadOnlySet<string>? AllowedSessionStartCidrs = null,
    int SourceFailedAttemptThreshold = SecurityAccessPolicy.DefaultSourceFailedAttemptThreshold)
{
    /// <summary>
    /// Failed attempts from one client IP address, across all identifiers, before that address is
    /// denied for the failed-attempt denial duration. Higher than the per-account threshold because
    /// one office address is often shared by several staff members.
    /// </summary>
    public const int DefaultSourceFailedAttemptThreshold = 20;
    public const int MaximumSourceFailedAttemptThreshold = 1000;

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
        if (SourceFailedAttemptThreshold is < 1 or > MaximumSourceFailedAttemptThreshold)
            throw new ArgumentOutOfRangeException(nameof(SourceFailedAttemptThreshold), "The per-address failed-attempt threshold must be between 1 and 1000.");
        if (!EnabledAuthenticationMechanisms.Contains(AuthenticationMethod.Password))
            throw new ArgumentException("Username/password authentication must remain enabled.", nameof(EnabledAuthenticationMechanisms));
        if (MaximumConcurrentSessions is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(MaximumConcurrentSessions), "The concurrent-session maximum must be between 1 and 3.");
        if (MaximumConcurrentSessions > 1 && !ConcurrentSessionsFunctionallyRequired)
            throw new ArgumentException("Concurrent sessions require a recorded functional need.", nameof(ConcurrentSessionsFunctionallyRequired));
        if (!SessionInactivityTimeout.IsWithinBounds(DefaultInactivityTimeoutMinutes))
            throw SessionInactivityTimeout.OutOfBounds(nameof(DefaultInactivityTimeoutMinutes));
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
        SourceFailedAttemptThreshold == other.SourceFailedAttemptThreshold &&
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
    PasswordRecovery,
    PhoneLogin,
    EmailLogin,
    PasskeyLogin,
    PasswordPlusPhoneOtp,
    /// <summary>Continue on a just-created tenant's own host with a one-time handoff ticket.</summary>
    TenantHandoff
}
