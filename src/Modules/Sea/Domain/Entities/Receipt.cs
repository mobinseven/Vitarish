namespace Vitarish.Sea.Domain.Entities;

public class Receipt : Entity
{
    private Guid _objectId = Guid.Empty;
    private string _readerId = string.Empty;

    public Guid ObjectId
    {
        get => _objectId;
        private set => SetField(ref _objectId, value, nameof(ObjectId));
    }

    public string ReaderId
    {
        get => _readerId;
        set => SetField(ref _readerId, value, nameof(ReaderId));
    }

    public Receipt()
    {
    }

    public Receipt(Guid objectId, string readerId)
    {
        Id = Guid.NewGuid();
        _objectId = objectId;
        _readerId = readerId;
    }
}
