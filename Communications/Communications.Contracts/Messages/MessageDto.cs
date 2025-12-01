namespace Vitarish.Communications.Contracts.Messages;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed record MessageDto(
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string?>? Metadata = null);
