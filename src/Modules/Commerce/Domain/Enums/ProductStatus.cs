namespace Vitarish.Commerce.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProductStatus>))]
public enum ProductStatus
{
    Draft,
    Proposed,
    Published,
    Rejected
}
