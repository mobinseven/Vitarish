using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Authentication.Infrastructure;

public sealed class SmsAuthenticationCodeSender(
    IMessagesService messagesService,
    VerificationSmsRequestShield shield) : IPhoneAuthenticationCodeSender
{
    public async Task SendAsync(string phoneNumber, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        if (!shield.CanSend(phoneNumber))
            throw new InvalidOperationException("SMS request was blocked by rate limiting.");

        Message message = new() { Recipient = phoneNumber, Body = code };
        await messagesService.SendSmsCode(message, cancellationToken);
    }
}
