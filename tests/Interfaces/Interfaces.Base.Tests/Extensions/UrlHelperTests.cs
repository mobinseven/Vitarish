using Vitarish.Interfaces.Base.Extensions;

namespace Interfaces.Base.Tests.Extensions;

public class UrlHelperTests
{
    [Fact]
    public void UrlHelper_BuildsAbsoluteUrl_FromBaseAndPath()
    {
        string result = UrlHelper.UrlCombine("https://example.com/api", "/v1/users");

        _ = result.Should().Be("https://example.com/api/v1/users");
    }

    [Fact]
    public void UrlHelper_EncodesQueryString()
    {
        string result = UrlHelper.UrlCombine("https://example.com/api", "search?q=hello world");

        _ = result.Should().Be("https://example.com/api/search?q=hello%20world");
    }
}
