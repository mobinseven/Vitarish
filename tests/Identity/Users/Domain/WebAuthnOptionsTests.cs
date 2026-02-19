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

        options.ServerName.Should().Be("Vitarish");
        options.Timeout.Should().Be(60000u);
        options.TimestampDriftTolerance.Should().Be(300000u);
        options.AttestationConveyance.Should().Be("none");
        options.UserVerification.Should().Be("preferred");
        options.RequireResidentKey.Should().Be("discouraged");
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

        options.ServerDomain.Should().Be("auth.example.com");
        options.Origins.Should().Contain("https://auth.example.com");
        options.ServerName.Should().Be("Auth Service");
        options.Timeout.Should().Be(120000u);
        options.TimestampDriftTolerance.Should().Be(600000u);
        options.AttestationConveyance.Should().Be("direct");
        options.AuthenticatorAttachment.Should().Be("platform");
        options.UserVerification.Should().Be("required");
        options.RequireResidentKey.Should().Be("required");
    }
}
