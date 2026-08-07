
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;
using Vitarish.Communications.Domain.Options;

namespace Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

public class SmsDotIrProvider : ISmsProvider
{
    private readonly HttpClient apiClient;
    private readonly SmsDotIrOptions options;

    public SmsDotIrProvider(IOptions<ProvidersOptions> providersOptions, IHttpClientFactory httpClientFactory)
    {
        options = providersOptions.Value.SmsDotIr;
        apiClient = httpClientFactory.CreateClient();
        apiClient.BaseAddress = new Uri("https://api.sms.ir/");
        apiClient.DefaultRequestHeaders.Add("X-API-KEY", options.Key);
        apiClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default)
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
            MessageTexts = [message.Body ?? string.Empty],
            Mobiles = [message.Recipient ?? string.Empty]
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

    public async Task SendCode(Message message, CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

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
            Mobile = message.Recipient ?? string.Empty,
            TemplateId = options.VerificationCodeTemplateId,
            Parameters = [new NameValue { Name = "CODE", Value = message.Body ?? string.Empty }]
        };

        using HttpResponseMessage response =
            await apiClient.PostAsJsonAsync("v1/send/verify", req, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr SendCode failed: {response.StatusCode} - {content}");
        }
    }

    public async Task SendReminder(string receipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default)
    {

        SendVerifyRequest req = new()
        {
            Mobile = receipient ?? string.Empty,
            TemplateId = options.ReminderTemplateId,
            Parameters = [
                new NameValue { Name = "PROJECT", Value = project ?? string.Empty },
                new NameValue { Name = "PROCEDURE", Value = procedure ?? string.Empty },
                new NameValue { Name = "EVENT", Value = evnt ?? string.Empty },
                new NameValue { Name = "TENANT", Value = tenant ?? string.Empty },
            ]
        };

        using HttpResponseMessage response =
            await apiClient.PostAsJsonAsync("v1/send/verify", req, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr SendReminder failed: {response.StatusCode} - {content}");
        }
    }

    public async Task<SmsSendResult> SendTemplatedSms(string recipient, string templateId, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required", nameof(recipient));
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Template ID is required", nameof(templateId));
        }

        NameValue[] paramsArray = parameters
            .Select(kv => new NameValue { Name = kv.Key, Value = kv.Value })
            .ToArray();

        SendVerifyRequest req = new()
        {
            Mobile = recipient,
            TemplateId = templateId,
            Parameters = paramsArray
        };

        using HttpResponseMessage response =
            await apiClient.PostAsJsonAsync("v1/send/verify", req, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"SmsDotIr SendTemplatedSms failed: {response.StatusCode} - {content}");
        }

        SmsDotIrSendResult result = await response.Content
            .ReadFromJsonAsync<SmsDotIrSendResult>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SmsDotIr templated send response did not include a payload.");
        SmsDotIrSendResultData data = result.Data
            ?? throw new InvalidOperationException("SmsDotIr templated send response did not include data payload.");
        if (result.PrimaryMessageId <= 0)
        {
            throw new InvalidOperationException("SmsDotIr templated send response did not include a positive message identifier.");
        }

        return new SmsSendResult(
            result.PrimaryMessageId.ToString(CultureInfo.InvariantCulture),
            recipient,
            data.Cost,
            data.PackId,
            SubmittedAt: null,
            RelatedMessageIds: data.MessageIds?.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray(),
            Metadata: BuildSendMetadata(result));
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
        Dictionary<string, string?> metadata = [];

        if (data.LineNumber.HasValue)
        {
            metadata["lineNumber"] = data.LineNumber.Value.ToString(CultureInfo.InvariantCulture);
        }

        return metadata.Count == 0 ? null : metadata;
    }

    private static DateTime? FromUnixTimeSeconds(long? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(value.Value).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    // Request DTOs for SmsDotIr API
    private sealed record SendLikeToLikeRequest
    {
        [JsonPropertyName("lineNumber")]
        public string LineNumber { get; init; } = string.Empty;

        [JsonPropertyName("messageTexts")]
        public string[] MessageTexts { get; init; } = Array.Empty<string>();

        [JsonPropertyName("mobiles")]
        public string[] Mobiles { get; init; } = Array.Empty<string>();
    }

    private sealed record SendVerifyRequest
    {
        [JsonPropertyName("mobile")]
        public string Mobile { get; init; } = string.Empty;

        [JsonPropertyName("templateId")]
        public string TemplateId { get; init; } = string.Empty;

        [JsonPropertyName("parameters")]
        public NameValue[] Parameters { get; init; } = Array.Empty<NameValue>();
    }

    private sealed record NameValue
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty;
    }

}
