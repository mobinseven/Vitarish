namespace Vitarish.Commerce.Domain.Entities;

public class Product : Entity
{
    public string MedusaId
    {
        get;
        set => SetField(ref field, value, nameof(MedusaId));
    } = string.Empty;

    public string Handle
    {
        get;
        set => SetField(ref field, value, nameof(Handle));
    } = string.Empty;

    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

    public string Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    } = string.Empty;

    public ProductStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = ProductStatus.Draft;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];
}
