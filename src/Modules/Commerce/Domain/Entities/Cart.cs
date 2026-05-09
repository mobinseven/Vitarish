namespace Vitarish.Commerce.Domain.Entities;

public class Cart : Entity
{
    public string MedusaId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaId));
    } = string.Empty;

    public string CustomerId
    {
        get;
        set => SetField(ref field, value, nameof(CustomerId));
    } = string.Empty;

    public CartStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = CartStatus.Open;
}
