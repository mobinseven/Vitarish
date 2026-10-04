using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Authentication.Infrastructure;

public sealed class EmailAuthenticationCodeSender(
    IMessagesService messagesService,
    AuthenticationOtpDeliveryService otpDelivery) : IEmailAuthenticationCodeSender
{
    public async Task SendAsync(string email, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Message message = new() { Recipient = email, Subject = "Your verification code", Body = code, HtmlBody = $"<p>Your verification code is: <strong>{code}</strong></p>" };
        await otpDelivery.SendEmailCodeAsync(email, () => messagesService.SendEmail(message, cancellationToken));
    }
}
