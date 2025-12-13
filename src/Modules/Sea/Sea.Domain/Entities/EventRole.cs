namespace Vitarish.Sea.Domain.Entities;

public class EventRole : Entity
{
    private Guid _eventId;
    public Guid EventId
    {
        get => _eventId;
        private set => SetField(ref _eventId, value, nameof(EventId));
    }

    private Guid _roleId;
    public Guid RoleId
    {
        get => _roleId;
        private set => SetField(ref _roleId, value, nameof(RoleId));
    }

    private RoleType _type;
    public RoleType Type
    {
        get => _type;
        private set => SetField(ref _type, value, nameof(Type));
    }

    public EventRole(Guid eventId, Guid roleId, RoleType type)
    {
        Id = Guid.NewGuid();
        _eventId = eventId;
        _roleId = roleId;
        _type = type;
    }
}
