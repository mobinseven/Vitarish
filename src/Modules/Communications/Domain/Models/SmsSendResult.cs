namespace Vitarish.Communications.Domain.Models;

public sealed record SmsSendResult(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("cost")] decimal Cost,
    [property: JsonPropertyName("packId")] Guid? PackId,
    [property: JsonPropertyName("submittedAt")] DateTimeOffset? SubmittedAt,
    [property: JsonPropertyName("relatedMessageIds")] IReadOnlyList<string>? RelatedMessageIds,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);
