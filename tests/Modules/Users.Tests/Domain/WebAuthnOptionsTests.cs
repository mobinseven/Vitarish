using Vitarish.Users.Domain.Options;

namespace Users.Tests.Domain;

public class WebAuthnOptionsTests
{
    [Fact]
    public void WebAuthnOptions_DefaultsMatchExpectedValues()
    {
        WebAuthnOptions options = new()
        {
            ServerDomain = "example.com",
            Origins = ["https://example.com"]
        };

        _ = options.ServerName.Should().Be("Vitarish");
        _ = options.Timeout.Should().Be(60000u);
        _ = options.TimestampDriftTolerance.Should().Be(300000u);
        _ = options.AttestationConveyance.Should().Be("none");
        _ = options.UserVerification.Should().Be("preferred");
        _ = options.RequireResidentKey.Should().Be("discouraged");
    }

    [Fact]
    public void WebAuthnOptions_AllowsCustomConfiguration()
    {
        WebAuthnOptions options = new()
        {
            ServerDomain = "auth.example.com",
            Origins = ["https://auth.example.com"],
            ServerName = "Auth Service",
            Timeout = 120000,
            TimestampDriftTolerance = 600000,
            AttestationConveyance = "direct",
            AuthenticatorAttachment = "platform",
            UserVerification = "required",
            RequireResidentKey = "required"
        };

        _ = options.ServerDomain.Should().Be("auth.example.com");
        _ = options.Origins.Should().Contain("https://auth.example.com");
        _ = options.ServerName.Should().Be("Auth Service");
        _ = options.Timeout.Should().Be(120000u);
        _ = options.TimestampDriftTolerance.Should().Be(600000u);
        _ = options.AttestationConveyance.Should().Be("direct");
        _ = options.AuthenticatorAttachment.Should().Be("platform");
        _ = options.UserVerification.Should().Be("required");
        _ = options.RequireResidentKey.Should().Be("required");
    }
}
