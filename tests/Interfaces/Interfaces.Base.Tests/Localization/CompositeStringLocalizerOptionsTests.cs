using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerOptionsTests
{
    [Fact]
    public void CompositeStringLocalizerOptions_Defaults_AreStable()
    {
        var options = new CompositeStringLocalizerOptions();

        options.ResourceTypesInPriorityOrder.Should().NotBeNull();
        options.ResourceTypesInPriorityOrder.Should().BeEmpty();
    }
}
