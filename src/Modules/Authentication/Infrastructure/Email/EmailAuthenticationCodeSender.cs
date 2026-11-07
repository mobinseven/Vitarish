using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Encodings.Web;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure.Email;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Authentication.Infrastructure;

public sealed class EmailAuthenticationCodeSender(
    IMessagesService messagesService,
    AuthenticationOtpDeliveryService otpDelivery,
    IOptions<DeliveredCodeLogOptions> deliveredCodeLog,
    IStringLocalizer<AuthenticationEmailCodeResources> resources,
    ILogger<EmailAuthenticationCodeSender> logger) : IEmailAuthenticationCodeSender
{
    private const string LegacyVerificationCodeSubject = "Your verification code";
    private const string LegacyVerificationCodeHtmlLeadIn = "Your verification code is:";

    public async Task SendAsync(string email, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        string encodedCode = HtmlEncoder.Default.Encode(code);
        string subject = LegacyVerificationCodeSubject;
        string body = code;
        string htmlBody = $"<p>{LegacyVerificationCodeHtmlLeadIn} <strong>{encodedCode}</strong></p>";
        if (purpose is AuthenticationChallengePurpose.PasswordLoginEmailOtp)
        {
            subject = resources[AuthenticationEmailResourceKeys.Subject];
            body = string.Format(CultureInfo.CurrentUICulture, resources[AuthenticationEmailResourceKeys.TextBody], code);
            htmlBody = string.Format(CultureInfo.CurrentUICulture, resources[AuthenticationEmailResourceKeys.HtmlBody], encodedCode);
        }

        Message message = new()
        {
            Recipient = email,
            Subject = subject,
            Body = body,
            HtmlBody = htmlBody
        };
        await otpDelivery.SendEmailCodeAsync(email, () => messagesService.SendEmail(message, cancellationToken));
        // Temporary exception, see DeliveredCodeLogOptions (issue #893).
        if (deliveredCodeLog.Value.Enabled && purpose is not AuthenticationChallengePurpose.PasswordLoginEmailOtp)
            logger.LogWarning("Delivered email sign-in code {Code} for {Purpose}", code, purpose);
    }
}

public static class AuthenticationEmailResourceKeys
{
    public const string Subject = "VerificationCodeSubject";
    public const string TextBody = "VerificationCodeTextBody";
    public const string HtmlBody = "VerificationCodeHtmlBody";
}
