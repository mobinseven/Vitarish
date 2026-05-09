namespace Vitarish.Commerce.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<CartStatus>))]
public enum CartStatus
{
    Open,
    Ordered,
    Abandoned
}
