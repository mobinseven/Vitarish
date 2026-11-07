using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Apps;

namespace Vitarish.Parties.Tests.Application;

public sealed class PlatformAppCatalogTests
{
    [Fact]
    public void Collects_registered_apps_in_the_catalog()
    {
        PlatformApp first = new("clinic", "Clinic", PlatformAppAvailability.Live, HostsWorkspaces: true, SelfService: null);
        PlatformApp second = new("landing", "Landing", PlatformAppAvailability.Live, HostsWorkspaces: false, SelfService: null);
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton(first)
            .AddSingleton(second)
            .AddSingleton<IPlatformAppCatalog, PlatformAppCatalog>()
            .BuildServiceProvider();

        services.GetRequiredService<IPlatformAppCatalog>().Apps.Should().Equal(first, second);
    }
}
