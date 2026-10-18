using FluentAssertions;
using MailKit.Security;
using Vitarish.Communications.Domain.Options;
using Vitarish.Communications.Infrastructure.Services;

namespace Main.Tests;

// Production email runs through Main's composition, so its SMTP transport choice is proven here.
public sealed class SmtpTransportSecurityTests
{
    [Theory]
    [InlineData(587, SecureSocketOptions.StartTls)]
    [InlineData(25, SecureSocketOptions.StartTls)]
    [InlineData(465, SecureSocketOptions.SslOnConnect)]
    public void Enabled_ssl_requires_tls_in_the_form_the_port_speaks(int port, SecureSocketOptions expected) =>
        SmtpTransportSecurity.For(new SmtpOptions { Port = port, EnableSsl = true }).Should().Be(expected);

    [Fact]
    public void Disabled_ssl_upgrades_only_when_the_server_offers_starttls() =>
        SmtpTransportSecurity.For(new SmtpOptions { Port = 587, EnableSsl = false }).Should().Be(SecureSocketOptions.StartTlsWhenAvailable);
}
