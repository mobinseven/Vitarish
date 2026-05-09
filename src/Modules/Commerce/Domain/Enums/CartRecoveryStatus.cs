namespace Vitarish.Commerce.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<CartRecoveryStatus>))]
public enum CartRecoveryStatus
{
    None,
    ContactPending,
    Contacted,
    Recovered,
    Ignored
}
