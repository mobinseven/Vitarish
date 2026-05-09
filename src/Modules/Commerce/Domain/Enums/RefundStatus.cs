namespace Vitarish.Commerce.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RefundStatus>))]
public enum RefundStatus
{
    None,
    Requested,
    Processing,
    Refunded,
    Rejected
}
