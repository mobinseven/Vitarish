using System.Text.Json.Serialization;

namespace Vitarish.Communications.Domain.Models;

public sealed record SmsDeliveryState(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("statusCode")] int? StatusCode,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("messageText")] string? MessageText,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("sentAt")] DateTimeOffset? SentAt,
    [property: JsonPropertyName("deliveredAt")] DateTimeOffset? DeliveredAt,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);
