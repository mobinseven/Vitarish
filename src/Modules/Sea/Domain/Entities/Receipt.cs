namespace Vitarish.Sea.Domain.Entities;

public class Receipt : Entity
{
    private Guid _objectId = Guid.Empty;

    public Guid ObjectId
    {
        get => _objectId;
        private set => SetField(ref _objectId, value, nameof(ObjectId));
    }

    public Receipt()
    {
    }

    public Receipt(Guid objectId)
    {
        Id = Guid.NewGuid();
        _objectId = objectId;
    }
}
