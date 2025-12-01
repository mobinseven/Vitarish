namespace Vitarish.Communications.Infrastructure.Messages.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;

public class MessagesService : IMessagesService
{
    private readonly CommunicationsOptions options;
    private readonly ISmsProvider smsProvider;

    public MessagesService(IOptions<CommunicationsOptions> options, ISmsProvider smsProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(smsProvider);
        this.options = options.Value;
        this.smsProvider = smsProvider;
    }

    public async Task SendEmailAsync(Message message, CancellationToken cancellationToken = default)
    {
        MimeMessage mimeMessage = new();
        mimeMessage.From.Add(new MailboxAddress(options.Smtp.Name, options.Smtp.Username));
        mimeMessage.To.Add(new MailboxAddress(message.Recipient ?? string.Empty, message.Recipient ?? string.Empty));
        mimeMessage.Subject = message.Subject;
        BodyBuilder builder = new()
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.Body,
        };
        mimeMessage.Body = builder.ToMessageBody();
        using SmtpClient client = new();
        await client.ConnectAsync(options.Smtp.Host, options.Smtp.Port, options.Smtp.EnableSsl, cancellationToken);
        await client.AuthenticateAsync(options.Smtp.Username, options.Smtp.Password, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendSmsAsync(message, cancellationToken);

    public Task SendSmsCodeAsync(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendCodeAsync(message, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
        smsProvider.GetSmsDeliveryStateAsync(messageId, cancellationToken);
}
