namespace Vitarish.Sea.Domain.Entities;

public sealed class ProjectObject : Entity
{
    public Guid ConcurrencyToken
    {
        get;
        set => SetField(ref field, value, nameof(ConcurrencyToken));
    }

    public string OwnerEntityType
    {
        get;
        set => SetField(ref field, value, nameof(OwnerEntityType));
    } = string.Empty;

    public Guid OwnerEntityId
    {
        get;
        set => SetField(ref field, value, nameof(OwnerEntityId));
    }

    public string ObjectType
    {
        get;
        set => SetField(ref field, value, nameof(ObjectType));
    } = string.Empty;

    public Dictionary<string, object?> Data
    {
        get;
        set => SetField(ref field, value, nameof(Data));
    } = [];
}
