using FluentAssertions;
using Moq;
using Vitarish.Parties.Application;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Vitarish.Landing.Application.Tests;

public sealed class LandingHostRoleFreeSessionPolicyTests
{
    [Fact]
    public async Task Admits_a_role_free_session_on_the_live_landing_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IAppTenantDirectory> tenants = new();
        tenants.Setup(directory => directory.IsLiveAppTenantAsync(tenantId, LandingTenant.AppId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        bool admitted = await Policy(tenants, tenantId.ToString("D")).AllowsRoleFreeSessionAsync();

        admitted.Should().BeTrue();
    }

    [Fact]
    public async Task Refuses_when_the_landing_host_is_bound_to_any_other_tenant()
    {
        Mock<IAppTenantDirectory> tenants = new();
        tenants.Setup(directory => directory.IsLiveAppTenantAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        bool admitted = await Policy(tenants, Guid.NewGuid().ToString("D")).AllowsRoleFreeSessionAsync();

        admitted.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-tenant")]
    public async Task Refuses_without_a_resolved_tenant(string? tenantId)
    {
        Mock<IAppTenantDirectory> tenants = new(MockBehavior.Strict);

        bool admitted = await Policy(tenants, tenantId).AllowsRoleFreeSessionAsync();

        admitted.Should().BeFalse();
    }

    private static LandingHostRoleFreeSessionPolicy Policy(Mock<IAppTenantDirectory> tenants, string? tenantId)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.TenantId).Returns(tenantId);
        return new(tenants.Object, context.Object);
    }
}
