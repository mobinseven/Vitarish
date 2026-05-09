namespace Vitarish.Commerce.Domain.Entities;

public class Store : Entity
{
    public string MedusaId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaId));
    } = string.Empty;

    public string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;

    public string CurrencyCode
    {
        get;
        set => SetField(ref field, value, nameof(CurrencyCode));
    } = "usd";

    public string DefaultRegionId
    {
        get;
        set => SetField(ref field, value, nameof(DefaultRegionId));
    } = string.Empty;

    public string MedusaBaseUrl
    {
        get;
        set => SetField(ref field, value, nameof(MedusaBaseUrl));
    } = string.Empty;

    public string AdminApiToken
    {
        get;
        set => SetField(ref field, value, nameof(AdminApiToken));
    } = string.Empty;

    public string StorefrontPublishableKey
    {
        get;
        set => SetField(ref field, value, nameof(StorefrontPublishableKey));
    } = string.Empty;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public StoreStats Stats { get; set; } = new();
}
