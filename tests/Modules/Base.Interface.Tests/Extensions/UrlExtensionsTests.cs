using Vitarish.Modules.Base.Interface.Extensions;

namespace Modules.Base.Interface.Tests.Extensions;

public class UrlExtensionsTests
{
    [Fact]
    public void UrlExtensions_AddQueryParam_AppendsCorrectly()
    {
        string result = "https://example.com/items".AddQueryParam("page", "2");

        _ = result.Should().Be("https://example.com/items?page=2");
    }

    [Fact]
    public void UrlExtensions_AddQueryParam_OverridesExisting()
    {
        string result = "https://example.com/items?page=1&filter=active".AddQueryParam("page", "3");

        _ = result.Should().Be("https://example.com/items?page=3&filter=active");
    }
}
