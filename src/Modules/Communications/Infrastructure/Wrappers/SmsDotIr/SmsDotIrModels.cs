using System.Text.Json.Serialization;

namespace Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

public sealed record SmsDotIrSendResult(
    [property: JsonPropertyName("data")] SmsDotIrSendResultData? Data,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("message")] string? Message)
{
    [JsonIgnore]
    public long PrimaryMessageId => Data?.MessageIds?.FirstOrDefault(id => id > 0) ?? 0;
}

public sealed record SmsDotIrSendResultData(
    [property: JsonPropertyName("packId")] Guid PackId,
    [property: JsonPropertyName("messageIds")] IReadOnlyList<long>? MessageIds,
    [property: JsonPropertyName("cost")] decimal Cost);

public sealed record SmsDotIrDeliveryState(
    [property: JsonPropertyName("data")] SmsDotIrDeliveryStateData? Data,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("message")] string? Message);

public sealed record SmsDotIrDeliveryStateData(
    [property: JsonPropertyName("messageId")] long MessageId,
    [property: JsonPropertyName("mobile")] string? Mobile,
    [property: JsonPropertyName("messageText")] string? MessageText,
    [property: JsonPropertyName("sendDateTime")] long? SendDateTime,
    [property: JsonPropertyName("lineNumber")] long? LineNumber,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("deliveryState")] int? DeliveryState,
    [property: JsonPropertyName("deliveryDateTime")] long? DeliveryDateTime);
