using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Communications.Domain.Options;

namespace Vitarish.Communications.Infrastructure.Services;

public class MessagesService : IMessagesService
{
    private readonly IOptionsMonitor<ProvidersOptions> providersOptions;
    private readonly ISmsProvider smsProvider;

    public MessagesService(IOptionsMonitor<ProvidersOptions> providersOptions, ISmsProvider smsProvider)
    {
        this.providersOptions = providersOptions ?? throw new ArgumentNullException(nameof(providersOptions));
        this.smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
    }

    public async Task SendEmail(Message message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        MimeMessage mimeMessage = new();
        ProvidersOptions currentOptions = providersOptions.CurrentValue;
        mimeMessage.From.Add(new MailboxAddress(currentOptions.Smtp.Name, currentOptions.Smtp.Username));
        mimeMessage.To.Add(new MailboxAddress(message.Recipient ?? string.Empty, message.Recipient ?? string.Empty));
        mimeMessage.Subject = message.Subject;
        BodyBuilder builder = new()
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.Body,
        };
        mimeMessage.Body = builder.ToMessageBody();
        using SmtpClient client = new();
        await client.ConnectAsync(currentOptions.Smtp.Host, currentOptions.Smtp.Port, currentOptions.Smtp.EnableSsl, cancellationToken);
        await client.AuthenticateAsync(currentOptions.Smtp.Username, currentOptions.Smtp.Password, cancellationToken);
        _ = await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendSms(message, cancellationToken);

    public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendCode(message, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
        smsProvider.GetSmsDeliveryStateAsync(messageId, cancellationToken);
}
