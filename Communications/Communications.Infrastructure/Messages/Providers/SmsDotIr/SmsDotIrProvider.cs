namespace Vitarish.Communications.Infrastructure.Messages.Providers.SmsDotIr;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;

public class SmsDotIrProvider : ISmsProvider
{
    private readonly HttpClient apiClient;
    private readonly SmsDotIrOptions options;

    public SmsDotIrProvider(IOptions<CommunicationsOptions> options)
    {
        this.options = options.Value.SmsDotIr;
        apiClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.sms.ir/")
        };
        apiClient.DefaultRequestHeaders.Add("X-API-KEY", this.options.Key);
        apiClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            throw new ArgumentException("Recipient is required", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Body))
        {
            throw new ArgumentException("Body is required", nameof(message));
        }

        SendLikeToLikeRequest req = new()
        {
            LineNumber = options.LineNumber,
            MessageTexts = [message.Body],
            Mobiles = [message.Recipient]
        };

        using HttpResponseMessage response =
            await apiClient.PostAsJsonAsync("v1/send/likeToLike", req, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr SendSms failed: {response.StatusCode} - {content}");
        }

        SmsDotIrSendResult result = await response.Content
            .ReadFromJsonAsync<SmsDotIrSendResult>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SmsDotIr send response did not include a payload.");

        SmsDotIrSendResultData data = result.Data
            ?? throw new InvalidOperationException("SmsDotIr send response did not include data payload.");

        if (data.MessageIds is not { Count: > 0 })
        {
            throw new InvalidOperationException("SmsDotIr send response did not include message identifiers.");
        }

        if (result.PrimaryMessageId <= 0)
        {
            throw new InvalidOperationException("SmsDotIr send response did not include a positive message identifier.");
        }

        SmsSendResult domainResult = new(
            result.PrimaryMessageId.ToString(CultureInfo.InvariantCulture),
            message.Recipient,
            data.Cost,
            data.PackId,
            SubmittedAt: null,
            RelatedMessageIds: data.MessageIds?.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray(),
            Metadata: BuildSendMetadata(result));

        return domainResult;
    }

    public async Task SendCodeAsync(Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            throw new ArgumentException("Recipient is required", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Body))
        {
            throw new ArgumentException("Code is required", nameof(message));
        }

        SendVerifyRequest req = new()
        {
            Mobile = message.Recipient,
            TemplateId = options.VerificationCodeTemplateId,
            Parameters = [new NameValue { Name = "CODE", Value = message.Body }]
        };

        using HttpResponseMessage response =
            await apiClient.PostAsJsonAsync("v1/send/verify", req, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr SendCode failed: {response.StatusCode} - {content}");
        }
    }

    public async Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default)
    {
        if (messageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(messageId), "Message ID must be a positive value.");
        }

        using HttpResponseMessage response =
            await apiClient.GetAsync($"v1/send/{messageId}", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr GetSmsDeliveryState failed: {response.StatusCode} - {content}");
        }

        SmsDotIrDeliveryState result = await response.Content
            .ReadFromJsonAsync<SmsDotIrDeliveryState>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SmsDotIr delivery response did not include a payload.");

        SmsDotIrDeliveryStateData data = result.Data
            ?? throw new InvalidOperationException("SmsDotIr delivery response did not include data payload.");

        if (data.MessageId <= 0)
        {
            throw new InvalidOperationException("SmsDotIr delivery response did not include a positive message identifier.");
        }

        SmsDeliveryState domainState = new(
            data.MessageId.ToString(CultureInfo.InvariantCulture),
            data.Mobile,
            data.DeliveryState,
            result.Message,
            data.MessageText,
            data.Cost,
            FromUnixTimeSeconds(data.SendDateTime),
            FromUnixTimeSeconds(data.DeliveryDateTime),
            BuildDeliveryMetadata(data));

        return domainState;
    }

    private static IReadOnlyDictionary<string, string?>? BuildSendMetadata(SmsDotIrSendResult result)
    {
        Dictionary<string, string?> metadata = new()
        {
            ["statusCode"] = result.Status.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            metadata["statusMessage"] = result.Message;
        }

        return metadata;
    }

    private static IReadOnlyDictionary<string, string?>? BuildDeliveryMetadata(SmsDotIrDeliveryStateData data)
    {
        Dictionary<string, string?> metadata = new();

        if (data.LineNumber.HasValue)
        {
            metadata["lineNumber"] = data.LineNumber.Value.ToString(CultureInfo.InvariantCulture);
        }

        return metadata.Count == 0 ? null : metadata;
    }

    private static DateTimeOffset? FromUnixTimeSeconds(long? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(value.Value).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private sealed record SendLikeToLikeRequest
    {
        [JsonPropertyName("lineNumber")]
        public string LineNumber { get; init; } = string.Empty;

        [JsonPropertyName("messageTexts")]
        public string[] MessageTexts { get; init; } = [];

        [JsonPropertyName("mobiles")]
        public string[] Mobiles { get; init; } = [];
    }

    private sealed record SendVerifyRequest
    {
        [JsonPropertyName("mobile")]
        public string Mobile { get; init; } = string.Empty;

        [JsonPropertyName("templateId")]
        public string TemplateId { get; init; } = string.Empty;

        [JsonPropertyName("parameters")]
        public NameValue[] Parameters { get; init; } = [];
    }

    private sealed record NameValue
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty;
    }
}
