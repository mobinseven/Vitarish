using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Security.Authentication;
using Vitarish.Communications.Domain.Options;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Communications.Infrastructure.Services;

public class MessagesService(IOptionsMonitor<ProvidersOptions> providersOptions, ISmsProvider smsProvider, INotificationTemplateResolver? templateResolver = null) : IMessagesService
{
    private readonly IOptionsMonitor<ProvidersOptions> providersOptions = providersOptions ?? throw new ArgumentNullException(nameof(providersOptions));
    private readonly ISmsProvider smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
    private readonly INotificationTemplateResolver? _templateResolver = templateResolver;

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
        client.SslProtocols = SslProtocols.Tls12;
        client.CheckCertificateRevocation = true;
        await client.ConnectAsync(currentOptions.Smtp.Host, currentOptions.Smtp.Port, currentOptions.Smtp.EnableSsl, cancellationToken);
        await client.AuthenticateAsync(currentOptions.Smtp.Username, currentOptions.Smtp.Password, cancellationToken);
        _ = await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default) =>
        smsProvider.SendSms(message, cancellationToken);

    public async Task SendSmsCode(Message message, CancellationToken cancellationToken = default)
    {
        // Try to resolve verification template from Platform locale records.
        // Fall back to config-based template if Platform templates are unavailable.
        if (_templateResolver is not null)
        {
            ResolvedNotificationTemplate? template = await _templateResolver.ResolveAsync(
                null, // Use default locale (fa-IR fallback)
                NotificationPurpose.VerificationCode,
                cancellationToken);

            if (template?.SmsTemplateId is not null)
            {
                Dictionary<string, string> parameters = new() { ["CODE"] = message.Body ?? string.Empty };
                await smsProvider.SendTemplatedSms(message.Recipient!, template.SmsTemplateId, parameters, cancellationToken);
                return;
            }
        }

        // Fallback to config-based template
        await smsProvider.SendCode(message, cancellationToken);
    }

    public Task SendSmsReminder(string receipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default) =>
        smsProvider.SendReminder(receipient, project, procedure, evnt, tenant, cancellationToken);

    public Task<SmsSendResult> SendTemplatedSms(string recipient, string templateId, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default) =>
        smsProvider.SendTemplatedSms(recipient, templateId, parameters, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default) =>
        smsProvider.GetSmsDeliveryStateAsync(messageId, cancellationToken);
}
