namespace Vitarish.Commerce.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<FulfillmentStatus>))]
public enum FulfillmentStatus
{
    NotFulfilled,
    PartiallyFulfilled,
    Fulfilled,
    PartiallyShipped,
    Shipped,
    PartiallyReturned,
    Returned,
    Canceled,
    Requires_Action
}
