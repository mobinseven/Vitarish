using FluentAssertions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminOptionsTests
{
    [Fact]
    public void Production_is_the_default_endpoint()
    {
        new TaminOptions().EffectiveEndpoint.Should().Be(TaminEndpointMode.Production);
    }

    [Fact]
    public void Legacy_sandbox_flag_selects_the_pilot_endpoint()
    {
        new TaminOptions { UseSandbox = true }.EffectiveEndpoint.Should().Be(TaminEndpointMode.Sandbox);
    }

    [Fact]
    public void Explicit_endpoint_is_honoured_without_the_legacy_flag()
    {
        new TaminOptions { Endpoint = TaminEndpointMode.Sandbox }.EffectiveEndpoint.Should().Be(TaminEndpointMode.Sandbox);
    }
}
