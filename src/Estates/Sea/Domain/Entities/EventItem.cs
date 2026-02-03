namespace Vitarish.Sea.Domain.Entities;

public abstract class EventItem : Entity
{
    private readonly string _metadata = "{}";

    public EventItem() { }
    protected EventItem(Guid eventId, string metadata = "{}")
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        //SetMetadata(metadata);
    }

    public Guid EventId { get; private set; }

    //public string Metadata
    //{
    //    get => _metadata;
    //    private set => SetField(ref _metadata, value, nameof(Metadata));
    //}

    //public void SetMetadata(string metadata)
    //{
    //    Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
    //}
}

