using Interfaces.Base.Tests.TestInfrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Interface.Localization;
using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerFactoryTests
{
    [Fact]
    public void CompositeStringLocalizerFactory_CreatesCompositeWithProvidedFactories()
    {
        var primaryLocalizer = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Value"] = "Primary"
        });
        var secondaryLocalizer = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Value"] = "Secondary"
        });
        var factory = new TrackingStringLocalizerFactory();
        factory.Register(typeof(PrimaryResource), primaryLocalizer);
        factory.Register(typeof(SecondaryResource), secondaryLocalizer);

        var options = Options.Create(new CompositeStringLocalizerOptions
        {
            ResourceTypesInPriorityOrder = [typeof(PrimaryResource), typeof(SecondaryResource)]
        });

        var compositeFactory = new CompositeStringLocalizerFactory(factory, options);

        IStringLocalizer localizer = compositeFactory.Create(typeof(SecondaryResource));

        localizer["Value"].Value.Should().Be("Secondary");
        factory.Created.Should().Contain(new[] { typeof(SecondaryResource).FullName!, typeof(PrimaryResource).FullName! });
    }
}
