using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;
using Vitarish.Clients.Modules.Communications.Abstractions;
using Vitarish.Clients.Modules.Communications.Abstractions.Models;
using Vitarish.Clients.Modules.Communications.Internal.Models;

namespace Vitarish.Clients.Modules.Communications.Internal;

public class MessageClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/communications";
        registration.ResourceId = "/hubs/messages";
    }
}

internal sealed class MessageClient(IIdentityClientSignalRFactory connectionFactory) : IMessageClient, IAsyncDisposable
{
    private HubConnection? connection;

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        connection ??= await connectionFactory.CreateConnection<MessageClientDefinition>(cancellationToken)
            .ConfigureAwait(false);

        if (connection.State != HubConnectionState.Connected)
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SendEmailAsync(
        string recipient,
        string? subject,
        string? body,
        string? htmlBody = null,
        string? sender = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        Message message = new()
        {
            Recipient = recipient,
            Subject = subject,
            Body = body,
            HtmlBody = htmlBody,
            Sender = sender
        };

        await connection!.InvokeAsync(nameof(SendEmailAsync), message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SmsSendResult> SendSmsAsync(
        string phone,
        string body,
        string? sender = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        Message message = new()
        {
            Recipient = phone,
            Body = body,
            Sender = sender
        };

        return await connection!.InvokeAsync<SmsSendResult>(nameof(SendSmsAsync), message, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SendSmsCodeAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        Message message = new()
        {
            Recipient = phone,
            Body = code
        };

        await connection!.InvokeAsync(nameof(SendSmsCodeAsync), message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection!.InvokeAsync<SmsDeliveryState>(nameof(GetSmsDeliveryStateAsync), messageId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
