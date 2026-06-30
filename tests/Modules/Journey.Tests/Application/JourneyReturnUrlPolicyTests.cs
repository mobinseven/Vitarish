using Vitarish.Journey.Application.Policies;

namespace Vitarish.Journey.Tests.Application;

public class JourneyReturnUrlPolicyTests
{
    private readonly JourneyReturnUrlPolicy _policy = new();

    [Theory]
    [InlineData("https://external.example")]
    [InlineData("//external.example")]
    [InlineData("\\external.example")]
    [InlineData("/%5cexternal.example")]
    [InlineData("%2f%2fexternal.example")]
    [InlineData(null)]
    [InlineData("")]
    public void Normalize_rejects_unsafe_or_missing_urls(string? url)
    {
        _policy.Normalize(url).Should().BeNull();
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/home")]
    [InlineData("/account/profile/complete")]
    [InlineData("/workspaces/select")]
    [InlineData("/apps/clinic/dashboard")]
    public void Normalize_accepts_local_platform_paths(string url)
    {
        _policy.Normalize(url).Should().Be(url);
    }
}
