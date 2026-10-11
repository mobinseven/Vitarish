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

    [Fact]
    public void Password_complexity_requirements_cannot_be_disabled()
    {
        Action act = () => new PasswordPolicy(8, false, true, true, true).ValidateConfiguration();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Product_evaluated_profile_keeps_password_and_phone_second_factor_only()
    {
        AuthenticationProfilePolicy.ProductEvaluated.AllowPasswordlessPhoneLogin.Should().BeFalse();
        AuthenticationProfilePolicy.ProductEvaluated.AllowPasswordlessEmailLogin.Should().BeFalse();
        AuthenticationProfilePolicy.ProductEvaluated.AllowPasskeyLogin.Should().BeFalse();
    }

    [Fact]
    public void Product_evaluated_policy_records_all_existing_enabled_mechanisms()
    {
        SecurityAccessPolicy.ProductEvaluated.EnabledAuthenticationMechanisms
            .Should().BeEquivalentTo([AuthenticationMethod.Password, AuthenticationMethod.Phone]);
        SecurityAccessPolicy.ProductEvaluated.PermittedPreAuthenticationActions
            .Should().BeEquivalentTo([
                PreAuthenticationAction.AuthenticationProfile,
                PreAuthenticationAction.PasswordLogin,
                PreAuthenticationAction.PasswordRecovery,
                PreAuthenticationAction.PasswordPlusPhoneOtp]);
    }

    [Fact]
    public void Profile_reconciliation_changes_only_profile_owned_mechanisms()
    {
        SecurityAccessPolicy customized = SecurityAccessPolicy.Default with { FailedAttemptThreshold = 9 };

        SecurityAccessPolicy reconciled = customized.ReconcileProfile(AuthenticationProfilePolicy.ProductEvaluated);

        reconciled.FailedAttemptThreshold.Should().Be(9);
        reconciled.EnabledAuthenticationMechanisms.Should().BeEquivalentTo(SecurityAccessPolicy.ProductEvaluated.EnabledAuthenticationMechanisms);
        reconciled.PermittedPreAuthenticationActions.Should().BeEquivalentTo(SecurityAccessPolicy.ProductEvaluated.PermittedPreAuthenticationActions);
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
