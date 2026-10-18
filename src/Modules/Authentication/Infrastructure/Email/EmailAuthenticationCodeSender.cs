using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Authentication.Infrastructure;

public sealed class EmailAuthenticationCodeSender(
    IMessagesService messagesService,
    AuthenticationOtpDeliveryService otpDelivery,
    IOptions<DeliveredCodeLogOptions> deliveredCodeLog,
    ILogger<EmailAuthenticationCodeSender> logger) : IEmailAuthenticationCodeSender
{
    public async Task SendAsync(string email, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Message message = new() { Recipient = email, Subject = "Your verification code", Body = code, HtmlBody = $"<p>Your verification code is: <strong>{code}</strong></p>" };
        await otpDelivery.SendEmailCodeAsync(email, () => messagesService.SendEmail(message, cancellationToken));
        // Temporary exception, see DeliveredCodeLogOptions (issue #893).
        if (deliveredCodeLog.Value.Enabled)
            logger.LogWarning("Delivered email sign-in code {Code} for {Purpose}", code, purpose);
    }
}
