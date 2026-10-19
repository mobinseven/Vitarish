using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;

namespace Vitarish.Parties.Tests.Application;

public sealed class GetTenantSubdomainAvailabilityQueryHandlerTests
{
    private const string BaseDomain = "clinic.example.test";

    [Fact]
    public async Task Reports_a_free_subdomain_as_available_with_its_full_hostname()
    {
        Mock<ITenantHostnameRegistry> registry = new();

        TenantSubdomainAvailability result = await Handle(registry, " Rezaei ");

        result.Should().Be(new TenantSubdomainAvailability("rezaei", "rezaei.clinic.example.test", TenantSubdomainAvailabilityStatus.Available));
        registry.Verify(item => item.IsClaimedAsync("rezaei.clinic.example.test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reports_a_claimed_hostname_as_taken()
    {
        Mock<ITenantHostnameRegistry> registry = new();
        registry.Setup(item => item.IsClaimedAsync("rezaei.clinic.example.test", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        TenantSubdomainAvailability result = await Handle(registry, "rezaei");

        result.Status.Should().Be(TenantSubdomainAvailabilityStatus.Taken);
        result.Hostname.Should().Be("rezaei.clinic.example.test");
    }

    [Theory]
    [InlineData("ab", TenantSubdomainAvailabilityStatus.Malformed)]
    [InlineData("dr_rezaei", TenantSubdomainAvailabilityStatus.Malformed)]
    [InlineData("admin", TenantSubdomainAvailabilityStatus.Reserved)]
    public async Task Refuses_malformed_and_reserved_subdomains_without_a_lookup(string candidate, TenantSubdomainAvailabilityStatus expected)
    {
        Mock<ITenantHostnameRegistry> registry = new(MockBehavior.Strict);

        TenantSubdomainAvailability result = await Handle(registry, candidate);

        result.Status.Should().Be(expected);
        result.Hostname.Should().BeNull();
    }

    [Fact]
    public async Task Fails_when_no_tenant_base_domain_is_configured()
    {
        GetTenantSubdomainAvailabilityQueryHandler handler = new(Mock.Of<ITenantHostnameRegistry>(), Options.Create(new TenantAddressOptions()));

        Func<Task> act = () => handler.Handle(new("rezaei"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static Task<TenantSubdomainAvailability> Handle(Mock<ITenantHostnameRegistry> registry, string subdomain) =>
        new GetTenantSubdomainAvailabilityQueryHandler(registry.Object, Options.Create(new TenantAddressOptions { TenantBaseDomain = BaseDomain }))
            .Handle(new GetTenantSubdomainAvailabilityQuery(subdomain), CancellationToken.None);
}
