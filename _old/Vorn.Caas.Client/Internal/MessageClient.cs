
using global::Vorn.Aaas.Client.Api.Abstractions;
using global::Vorn.Caas.Client.Abstractions.Models;
using global::Vorn.Caas.Client.Internal.Models;
using System.Net.Http.Json;
using Vorn.Caas.Client.Abstractions;

namespace Vorn.Caas.Client.Internal;

internal class MessageClient(IApiClientService apiClientService) : IMessageClient
{
    private HttpClient httpClient = default!;
    private async Task EnsureHttpClient()
    {
        httpClient ??= await apiClientService.CreateClient("caas", "/api/v1/caas/");
    }

    public async Task SendSmsCodeAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        await EnsureHttpClient();
        Message message = new() { Body = code, Recipient = phone };
        using HttpRequestMessage request = new(HttpMethod.Post, "messages/smscode")
        {
            Content = JsonContent.Create(message)
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<SmsSendResult> SendSmsAsync(string phone, string body, string? sender = null, CancellationToken cancellationToken = default)
    {
        await EnsureHttpClient();
        Message message = new() { Recipient = phone, Body = body, Sender = sender };
        using HttpRequestMessage request = new(HttpMethod.Post, "messages/sms")
        {
            Content = JsonContent.Create(message)
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        SmsSendResult? result = await response.Content
            .ReadFromJsonAsync<SmsSendResult>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("SMS send response payload was empty.");
    }

    public async Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"messages/sms/{messageId}");
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        SmsDeliveryState? state = await response.Content
            .ReadFromJsonAsync<SmsDeliveryState>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return state ?? throw new InvalidOperationException("Delivery state response payload was empty.");
    }

    public async Task SendEmailAsync(string recipient, string? subject, string? body,
        string? htmlBody = null, string? sender = null, CancellationToken cancellationToken = default)
    {
        await EnsureHttpClient();
        Message message = new()
        {
            Recipient = recipient,
            Subject = subject,
            Body = body,
            HtmlBody = htmlBody,
            Sender = sender
        };
        using HttpRequestMessage request = new(HttpMethod.Post, "messages/email")
        {
            Content = JsonContent.Create(message)
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }
}