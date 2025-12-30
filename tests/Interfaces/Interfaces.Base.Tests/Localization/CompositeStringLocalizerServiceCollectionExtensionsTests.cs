using Interfaces.Base.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompositeStringLocalizer_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        _ = services.ConfigureCompositeStringLocalizer(new[] { typeof(PrimaryResource) });

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ResourceManagerStringLocalizerFactory));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IStringLocalizerFactory));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(MudLocalizer)
            && descriptor.ImplementationType == typeof(CustomMudLocalizer));
    }
}
