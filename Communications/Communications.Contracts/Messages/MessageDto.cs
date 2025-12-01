namespace Vitarish.Communications.Contracts.Messages;

using System.Text.Json.Serialization;

public sealed record MessageDto(
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("subject")] string? Subject = null,
    [property: JsonPropertyName("body")] string? Body = null,
    [property: JsonPropertyName("htmlBody")] string? HtmlBody = null,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata = null);
