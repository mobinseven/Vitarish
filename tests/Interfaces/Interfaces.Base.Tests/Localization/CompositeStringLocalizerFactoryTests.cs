using Interfaces.Base.Tests.TestInfrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerFactoryTests
{
    [Fact]
    public void CompositeStringLocalizerFactory_CreatesCompositeWithProvidedFactories()
    {
        TestStringLocalizer primaryLocalizer = new(new Dictionary<string, string>
        {
            ["Value"] = "Primary"
        });
        TestStringLocalizer secondaryLocalizer = new(new Dictionary<string, string>
        {
            ["Value"] = "Secondary"
        });
        TrackingStringLocalizerFactory factory = new();
        factory.Register(typeof(PrimaryResource), primaryLocalizer);
        factory.Register(typeof(SecondaryResource), secondaryLocalizer);

        var options = Options.Create(new CompositeStringLocalizerOptions
        {
            ResourceTypesInPriorityOrder = [typeof(PrimaryResource), typeof(SecondaryResource)]
        });

        CompositeStringLocalizerFactory compositeFactory = new(factory, options);

        IStringLocalizer localizer = compositeFactory.Create(typeof(SecondaryResource));

        _ = localizer["Value"].Value.Should().Be("Secondary");
        _ = factory.Created.Should().Contain(new[] { typeof(SecondaryResource).FullName!, typeof(PrimaryResource).FullName! });
    }
}
