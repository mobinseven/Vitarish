namespace Vitarish.Communications.Core.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Result of sending an SMS message.
/// </summary>
/// <param name="MessageId">The unique identifier of the sent message.</param>
/// <param name="Recipient">The recipient's phone number.</param>
/// <param name="Cost">The cost of sending the message.</param>
/// <param name="PackId">Optional pack identifier.</param>
/// <param name="SubmittedAt">The timestamp when the message was submitted.</param>
/// <param name="RelatedMessageIds">Related message identifiers for multi-part messages.</param>
/// <param name="Metadata">Additional metadata about the message.</param>
public sealed record SmsSendResult(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("cost")] decimal Cost,
    [property: JsonPropertyName("packId")] Guid? PackId,
    [property: JsonPropertyName("submittedAt")] DateTimeOffset? SubmittedAt,
    [property: JsonPropertyName("relatedMessageIds")] IReadOnlyList<string>? RelatedMessageIds,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata);
