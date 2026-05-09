namespace Vitarish.Commerce.Domain.Entities;

public class CartItem : Entity
{
    public Guid CartId { get; set; }

    public string VariantId
    {
        get;
        set => SetField(ref field, value, nameof(VariantId));
    } = string.Empty;

    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

    public int Quantity
    {
        get;
        set => SetField(ref field, value, nameof(Quantity));
    }

    public decimal UnitPrice
    {
        get;
        set => SetField(ref field, value, nameof(UnitPrice));
    }
}
