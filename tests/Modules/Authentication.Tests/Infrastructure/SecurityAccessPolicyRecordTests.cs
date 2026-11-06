using FluentAssertions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class SecurityAccessPolicyRecordTests
{
    [Fact]
    public void Persistence_record_round_trips_the_policy_values()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default;

        SecurityAccessPolicy result = SecurityAccessPolicyRecord.FromDomain(policy).ToDomain().ReconcileProfile(AuthenticationProfilePolicy.Default);

        result.Should().BeEquivalentTo(policy.ReconcileProfile(AuthenticationProfilePolicy.Default));
    }

    [Fact]
    public void Persistence_record_round_trips_the_per_address_threshold()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with { SourceFailedAttemptThreshold = 7 };

        SecurityAccessPolicy result = SecurityAccessPolicyRecord.FromDomain(policy).ToDomain();

        result.SourceFailedAttemptThreshold.Should().Be(7);
        result.EquivalentTo(SecurityAccessPolicy.Default).Should().BeFalse("a threshold change is a policy change");
    }

    [Fact]
    public void Email_second_factor_action_is_profile_derived_and_not_written_as_an_unknown_enum_value()
    {
        SecurityAccessPolicyRecord record = SecurityAccessPolicyRecord.FromDomain(SecurityAccessPolicy.Default);

        record.PermittedPreAuthenticationActions.Should().NotContain(nameof(PreAuthenticationAction.PasswordPlusEmailOtp));
        record.ToDomain().PermittedPreAuthenticationActions.Should().NotContain(PreAuthenticationAction.PasswordPlusEmailOtp);
        record.ToDomain().ReconcileProfile(AuthenticationProfilePolicy.Default)
            .PermittedPreAuthenticationActions.Should().Contain(PreAuthenticationAction.PasswordPlusEmailOtp);
    }

    [Fact]
    public void Persistence_record_round_trips_normalized_cidrs()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal)
            {
                "10.0.0.0/24",
                "2001:db8::/64"
            }
        };

        SecurityAccessPolicy result = SecurityAccessPolicyRecord.FromDomain(policy).ToDomain();

        result.AllowedSessionStartCidrs.Should().BeEquivalentTo(policy.AllowedSessionStartCidrs);
    }

    [Fact]
    public void Policy_validation_rejects_noncanonical_cidrs()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.7/24" }
        };

        Action act = policy.ValidateConfiguration;

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Applying_a_policy_increments_the_persisted_version_once()
    {
        SecurityAccessPolicyRecord record = SecurityAccessPolicyRecord.FromDomain(SecurityAccessPolicy.Default);

        record.Apply(SecurityAccessPolicy.Default);

        SecurityAccessPolicyState state = record.ToState();
        state.Version.Should().Be(1);
        state.Policy.ReconcileProfile(AuthenticationProfilePolicy.Default)
            .Should().BeEquivalentTo(SecurityAccessPolicy.Default.ReconcileProfile(AuthenticationProfilePolicy.Default));
    }

}
