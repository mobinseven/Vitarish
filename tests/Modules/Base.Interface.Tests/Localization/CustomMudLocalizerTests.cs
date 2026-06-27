using Modules.Base.Interface.Tests.TestInfrastructure;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Vitarish.Modules.Base.Interface.Localization;

namespace Modules.Base.Interface.Tests.Localization;

public class CustomMudLocalizerTests
{
    [Fact]
    public void CustomMudLocalizer_ReturnsLocalizedString_WhenPresent()
    {
        IStringLocalizer localizer = new TestStringLocalizer(new Dictionary<string, string>
        {
            ["Label"] = "Localized"
        });

        MudLocalizer mudLocalizer = new CustomMudLocalizer(localizer);

        _ = mudLocalizer["Label"].Value.Should().Be("Localized");
        _ = mudLocalizer["Label"].ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void CustomMudLocalizer_FallsBack_WhenMissing()
    {
        IStringLocalizer localizer = new TestStringLocalizer(new Dictionary<string, string>());

        MudLocalizer mudLocalizer = new CustomMudLocalizer(localizer);

        _ = mudLocalizer["Missing"].Value.Should().Be("Missing");
        _ = mudLocalizer["Missing"].ResourceNotFound.Should().BeTrue();
    }
}
