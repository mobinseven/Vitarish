using FluentAssertions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Authentication.Tests.Domain;

public sealed class SecurityAccessPolicyTests
{
    [Fact]
    public void Password_policy_requires_the_stage_three_complexity_baseline()
    {
        PasswordPolicy policy = PasswordPolicy.Default;

        policy.MinimumLength.Should().Be(8);
        policy.RequireLowercase.Should().BeTrue();
        policy.RequireUppercase.Should().BeTrue();
        policy.RequireNumber.Should().BeTrue();
        policy.RequireSpecialCharacter.Should().BeTrue();
        policy.Validate("Abcdef1!").Should().BeEmpty();
        policy.Validate("abcdef1!").Should().Contain(PasswordPolicyViolation.Uppercase);
    }

    [Fact]
    public void Password_policy_cannot_be_configured_below_eight_characters()
    {
        Action act = () => new PasswordPolicy(7, true, true, true, true).ValidateConfiguration();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SecurityAccessPolicy.MaximumSourceFailedAttemptThreshold + 1)]
    public void Per_address_failed_attempt_threshold_must_stay_within_bounds(int threshold)
    {
        Action act = () => (SecurityAccessPolicy.Default with { SourceFailedAttemptThreshold = threshold }).ValidateConfiguration();

        act.Should().Throw<ArgumentOutOfRangeException>();
        SecurityAccessPolicy.Default.SourceFailedAttemptThreshold.Should().Be(20);
    }

    [Fact]
    public void Password_complexity_requirements_cannot_be_disabled()
    {
        Action act = () => new PasswordPolicy(8, false, true, true, true).ValidateConfiguration();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Default_policy_offers_every_sign_in_method_and_both_password_second_factor_channels()
    {
        SecurityAccessPolicy.Default.EnabledAuthenticationMechanisms
            .Should().BeEquivalentTo([AuthenticationMethod.Password, AuthenticationMethod.Phone, AuthenticationMethod.Email, AuthenticationMethod.Passkey]);
        SecurityAccessPolicy.Default.PermittedPreAuthenticationActions
            .Should().Contain([PreAuthenticationAction.PasskeyLogin, PreAuthenticationAction.PasswordPlusPhoneOtp, PreAuthenticationAction.PasswordPlusEmailOtp]);
        AuthenticationProfilePolicy.Default.RequiredPreAuthenticationActions
            .Should().Contain([PreAuthenticationAction.PasswordPlusPhoneOtp, PreAuthenticationAction.PasswordPlusEmailOtp]);
        AuthenticationProfilePolicy.Default.RequiredAuthenticationMechanisms.Should().Contain(AuthenticationMethod.Email);
    }

    [Fact]
    public void Profile_reconciliation_changes_only_profile_owned_mechanisms()
    {
        AuthenticationProfilePolicy restricted = new AuthenticationProfilePolicy(UsePhoneOtpForTwoFactorAccounts: true, AllowPasswordlessPhoneLogin: false, AllowPasswordlessEmailLogin: false, AllowPasskeyLogin: false);
        SecurityAccessPolicy customized = SecurityAccessPolicy.Default with { FailedAttemptThreshold = 9 };

        SecurityAccessPolicy reconciled = customized.ReconcileProfile(restricted);

        reconciled.FailedAttemptThreshold.Should().Be(9);
        reconciled.EnabledAuthenticationMechanisms.Should().BeEquivalentTo(SecurityAccessPolicy.ForProfile(restricted).EnabledAuthenticationMechanisms);
        reconciled.PermittedPreAuthenticationActions.Should().BeEquivalentTo(SecurityAccessPolicy.ForProfile(restricted).PermittedPreAuthenticationActions);
    }

    [Fact]
    public void Default_idle_expiry_is_fourteen_days()
    {
        SecurityAccessPolicy.Default.DefaultInactivityTimeoutMinutes.Should().Be(20160);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(129600)]
    public void Idle_expiry_accepts_one_minute_through_ninety_days(int minutes)
    {
        Action act = () => (SecurityAccessPolicy.Default with { DefaultInactivityTimeoutMinutes = minutes }).ValidateConfiguration();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(129601)]
    public void Idle_expiry_outside_one_minute_through_ninety_days_is_rejected(int minutes)
    {
        Action act = () => (SecurityAccessPolicy.Default with { DefaultInactivityTimeoutMinutes = minutes }).ValidateConfiguration();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Challenge_policy_can_use_the_persisted_attempt_threshold()
    {
        AuthenticationChallengePolicy policy = new(TimeSpan.FromMinutes(5), 3);

        policy.CreatePhoneLoginChallenge("user-1", "+123", DateTimeOffset.UtcNow)
            .MaximumAttempts.Should().Be(3);
    }
}
