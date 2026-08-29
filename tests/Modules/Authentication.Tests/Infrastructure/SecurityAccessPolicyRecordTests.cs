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
    public void Applying_a_policy_increments_the_persisted_version_once()
    {
        SecurityAccessPolicyRecord record = SecurityAccessPolicyRecord.FromDomain(SecurityAccessPolicy.Default);

        record.Apply(SecurityAccessPolicy.ProductEvaluated);

        SecurityAccessPolicyState state = record.ToState();
        state.Version.Should().Be(1);
        state.Policy.Should().BeEquivalentTo(SecurityAccessPolicy.ProductEvaluated);
    }

}
