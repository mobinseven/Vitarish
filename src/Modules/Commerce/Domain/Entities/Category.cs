namespace Vitarish.Commerce.Domain.Entities;

public class Category : Entity
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

    public string Handle
    {
        get;
        set => SetField(ref field, value, nameof(Handle));
    } = string.Empty;

    public Guid? ParentCategoryId
    {
        get;
        set => SetField(ref field, value, nameof(ParentCategoryId));
    }
}
