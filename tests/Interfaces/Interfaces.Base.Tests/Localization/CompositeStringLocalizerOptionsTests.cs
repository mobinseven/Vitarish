using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerOptionsTests
{
    [Fact]
    public void CompositeStringLocalizerOptions_Defaults_AreStable()
    {
        CompositeStringLocalizerOptions options = new();

        _ = options.ResourceTypesInPriorityOrder.Should().NotBeNull();
        _ = options.ResourceTypesInPriorityOrder.Should().BeEmpty();
    }
}
