using Interfaces.Base.Tests.TestInfrastructure;
using Microsoft.Extensions.Localization;
using Vitarish.Interfaces.Base.Localization;

namespace Interfaces.Base.Tests.Localization;

public class CompositeStringLocalizerTests
{
    [Fact]
    public void CompositeStringLocalizer_ReturnsFirstMatch()
    {
        IStringLocalizer first = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Greeting"] = "Hello"
        });
        IStringLocalizer second = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Greeting"] = "Hi"
        });

        var composite = new CompositeStringLocalizer([first, second]);

        composite["Greeting"].Value.Should().Be("Hello");
        composite["Greeting"].ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void CompositeStringLocalizer_FallsBackToNextLocalizer_WhenMissing()
    {
        IStringLocalizer first = new TestStringLocalizer(new Dictionary<string, string>());
        IStringLocalizer second = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Greeting"] = "Hi"
        });

        var composite = new CompositeStringLocalizer([first, second]);

        composite["Greeting"].Value.Should().Be("Hi");
        composite["Greeting"].ResourceNotFound.Should().BeFalse();
    }
}
