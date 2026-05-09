namespace Vitarish.Commerce.Domain.Entities;

public class CustomerProfile : Entity
{
    public string UserId
    {
        get;
        set => SetField(ref field, value, nameof(UserId));
    } = string.Empty;

    public string MedusaCustomerId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaCustomerId));
    } = string.Empty;
}
