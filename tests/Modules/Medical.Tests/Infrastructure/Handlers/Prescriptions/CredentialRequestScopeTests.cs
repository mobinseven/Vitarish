using FluentAssertions;
using Moq;
using Vitarish.Medical.Infrastructure.Handlers.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Medical.Tests.Infrastructure.Handlers.Prescriptions;

public sealed class CredentialRequestScopeTests
{
    [Fact]
    public void Matches_only_the_authenticated_owner_and_tenant()
    {
        Guid doctorId = Guid.NewGuid();
        Guid clinicId = Guid.NewGuid();
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.OwnerId).Returns(doctorId.ToString());
        _ = context.SetupGet(value => value.TenantId).Returns(clinicId.ToString());

        CredentialRequestScope.Matches(context.Object, doctorId, clinicId).Should().BeTrue();
        CredentialRequestScope.Matches(context.Object, Guid.NewGuid(), clinicId).Should().BeFalse();
        CredentialRequestScope.Matches(context.Object, doctorId, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void TryResolve_rejects_missing_or_malformed_claims()
    {
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.OwnerId).Returns("not-a-guid");
        _ = context.SetupGet(value => value.TenantId).Returns(Guid.NewGuid().ToString());

        CredentialRequestScope.TryResolve(context.Object, out _, out _).Should().BeFalse();
    }
}
