namespace Vorn.Caas.Host.Presentation.Dto;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed record SmsSendResultDto(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("cost")] decimal Cost,
    [property: JsonPropertyName("packId")] Guid? PackId,
    [property: JsonPropertyName("submittedAt")] DateTimeOffset? SubmittedAt,
    [property: JsonPropertyName("relatedMessageIds")] IReadOnlyList<string>? RelatedMessageIds,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);

public sealed record SmsDeliveryStateDto(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("statusCode")] int? StatusCode,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("messageText")] string? MessageText,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("sentAt")] DateTimeOffset? SentAt,
    [property: JsonPropertyName("deliveredAt")] DateTimeOffset? DeliveredAt,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);
