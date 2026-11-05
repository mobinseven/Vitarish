using FluentAssertions;
using Vitarish.Audit.Domain;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class SecurityAuditSignInActorTests
{
    [Theory]
    [InlineData(SecurityAuditOutcome.Succeeded, "subject")]
    [InlineData(SecurityAuditOutcome.Started, null)]
    [InlineData(SecurityAuditOutcome.Pending, null)]
    [InlineData(SecurityAuditOutcome.Denied, null)]
    [InlineData(SecurityAuditOutcome.Failed, null)]
    public void Anonymous_attempt_is_attributed_to_the_subject_only_when_it_succeeded(string outcome, string? expectedActor) =>
        SecurityAuditSignInActor.Resolve(null, outcome, "subject").Should().Be(expectedActor);

    [Fact]
    public void Signed_in_request_keeps_its_own_actor() =>
        SecurityAuditSignInActor.Resolve("administrator", SecurityAuditOutcome.Succeeded, "subject").Should().Be("administrator");
}
