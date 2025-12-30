using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Accounts.Infrastructure.Extensions;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Infrastructure.Tests.Extensions;

public class BlazorSsrRedirectManagerExtensionsTests
{
    [Fact]
    public void BlazorSsrRedirectManagerExtensions_AddBlazorSsrRedirectManager_RegistersServices()
    {
        ServiceCollection services = new();

        _ = services.AddBlazorSsrRedirectManager();
        _ = services.AddLogging();
        _ = services.AddScoped<NavigationManager>(_ => new RecordingNavigationManager());

        ServiceProvider provider = services.BuildServiceProvider();
        object? resolved = provider.GetService(typeof(IdentityRedirectManager));

        resolved.Should().NotBeNull();
    }
}
