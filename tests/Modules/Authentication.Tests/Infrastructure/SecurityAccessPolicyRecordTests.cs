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
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;

        SecurityAccessPolicy result = SecurityAccessPolicyRecord.FromDomain(policy).ToDomain();

        result.Should().BeEquivalentTo(policy);
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

        record.Apply(SecurityAccessPolicy.ProductEvaluated);

        SecurityAccessPolicyState state = record.ToState();
        state.Version.Should().Be(1);
        state.Policy.Should().BeEquivalentTo(SecurityAccessPolicy.ProductEvaluated);
    }

}
