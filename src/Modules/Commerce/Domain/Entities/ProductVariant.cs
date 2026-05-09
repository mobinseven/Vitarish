namespace Vitarish.Commerce.Domain.Entities;

public class ProductVariant : Entity
{
    public string MedusaId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaId));
    } = string.Empty;

    public Guid ProductId { get; set; }

    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

    public string Sku
    {
        get;
        set => SetField(ref field, value, nameof(Sku));
    } = string.Empty;

    public decimal Price
    {
        get;
        set => SetField(ref field, value, nameof(Price));
    }

    public string CurrencyCode
    {
        get;
        set => SetField(ref field, value, nameof(CurrencyCode));
    } = "usd";

    public int InventoryQuantity
    {
        get;
        set => SetField(ref field, value, nameof(InventoryQuantity));
    }
}
