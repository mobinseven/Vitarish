using Vitarish.Interfaces.Base.Extensions;

namespace Interfaces.Base.Tests.Extensions;

public class NumberExtensionsTests
{
    [Fact]
    public void NumberExtensions_ToCompactString_FormatsThousands()
    {
        string result = 1200m.ToCompactString();

        _ = result.Should().Be("1.2K");
    }

    [Fact]
    public void NumberExtensions_ToCompactString_FormatsMillions()
    {
        string result = 2500000m.ToCompactString();

        _ = result.Should().Be("2.5M");
    }
}
