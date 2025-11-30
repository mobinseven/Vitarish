namespace Vorn.Caas.Client.Abstractions.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed record SmsSendResult(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("cost")] decimal Cost,
    [property: JsonPropertyName("packId")] Guid? PackId,
    [property: JsonPropertyName("submittedAt")] DateTimeOffset? SubmittedAt,
    [property: JsonPropertyName("relatedMessageIds")] IReadOnlyList<string>? RelatedMessageIds,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);
