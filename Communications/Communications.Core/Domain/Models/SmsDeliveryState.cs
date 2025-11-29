namespace Vitarish.Communications.Core.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Delivery state of an SMS message.
/// </summary>
/// <param name="MessageId">The unique identifier of the message.</param>
/// <param name="Recipient">The recipient's phone number.</param>
/// <param name="StatusCode">The status code from the SMS provider.</param>
/// <param name="Status">The human-readable status.</param>
/// <param name="MessageText">The message text that was sent.</param>
/// <param name="Cost">The cost of the message.</param>
/// <param name="SentAt">When the message was sent.</param>
/// <param name="DeliveredAt">When the message was delivered.</param>
/// <param name="Metadata">Additional metadata.</param>
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
