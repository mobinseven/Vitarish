namespace Vorn.Communications.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Vorn.Communications.Abstractions;
using Vorn.Communications.Models;
using Vorn.Communications.Options;

public class MessagesService : IMessagesService
{
    private readonly VornOptions options;
    private readonly ISmsProvider smsProvider;

    public MessagesService(VornOptions options, ISmsProvider smsProvider)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
    }

    public async Task SendEmail(Message message)
    {
        MimeMessage mimeMessage = new();
        mimeMessage.From.Add(new MailboxAddress(options.Providers.Smtp.Name, options.Providers.Smtp.Username));
        mimeMessage.To.Add(new MailboxAddress(message.Recipient ?? string.Empty, message.Recipient ?? string.Empty));
        mimeMessage.Subject = message.Subject;
        BodyBuilder builder = new()
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.Body,
        };
        mimeMessage.Body = builder.ToMessageBody();
        using SmtpClient client = new();
        await client.ConnectAsync(options.Providers.Smtp.Host, options.Providers.Smtp.Port, options.Providers.Smtp.EnableSsl);
        await client.AuthenticateAsync(options.Providers.Smtp.Username, options.Providers.Smtp.Password);
        await client.SendAsync(mimeMessage);
        await client.DisconnectAsync(true);
    }

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendSms(message, cancellationToken);

    public Task SendSmsCode(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendCode(message, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
        smsProvider.GetSmsDeliveryStateAsync(messageId, cancellationToken);
}
