using MailKit.Security;
using Vitarish.Communications.Domain.Options;

namespace Vitarish.Communications.Infrastructure.Services;

/// <summary>
/// Chooses how the SMTP connection is secured. <see cref="SmtpOptions.EnableSsl"/>
/// requires TLS: implicit TLS on the submissions port (RFC 8314), and a required
/// STARTTLS upgrade on any other port such as 587. Without it the connection
/// upgrades only when the server offers STARTTLS.
/// </summary>
public static class SmtpTransportSecurity
{
    public const int ImplicitTlsPort = 465;

    public static SecureSocketOptions For(SmtpOptions smtp) =>
        !smtp.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable
        : smtp.Port == ImplicitTlsPort ? SecureSocketOptions.SslOnConnect
        : SecureSocketOptions.StartTls;
}
