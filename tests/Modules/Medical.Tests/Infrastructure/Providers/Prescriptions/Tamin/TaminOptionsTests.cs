using FluentAssertions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminOptionsTests
{
    [Fact]
    public void V3_specific_base_urls_override_legacy_base_url()
    {
        TaminOptions options = new()
        {
            BaseUrl = "https://legacy.example",
            AccountBaseUrl = "https://account.example",
            SoaBaseUrl = "https://soa.example",
            ApiBaseUrl = "https://api.example"
        };

        options.EffectiveAccountBaseUrl.Should().Be("https://account.example");
        options.EffectiveSoaBaseUrl.Should().Be("https://soa.example");
        options.EffectiveApiBaseUrl.Should().Be("https://api.example");
    }

    [Fact]
    public void Legacy_sandbox_base_url_remains_a_compatibility_fallback()
    {
        TaminOptions options = new()
        {
            BaseUrl = "https://production.example",
            SandboxBaseUrl = "https://pilot.example",
            UseSandbox = true
        };

        options.EffectiveSoaBaseUrl.Should().Be("https://pilot.example");
    }
}
