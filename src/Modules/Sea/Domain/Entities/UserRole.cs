namespace Vitarish.Sea.Domain.Entities;

public class UserRole : Entity
{
    private Guid _userId = Guid.Empty;
    public Guid UserId
    {
        get => _userId;
        private set => SetField(ref _userId, value, nameof(UserId));
    }

    private Guid _roleId = Guid.Empty;
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
    public UserRole() { }
    public UserRole(Guid userId, Guid roleId, RoleType type)
    {
        Id = Guid.NewGuid();
        _userId = userId;
        _roleId = roleId;
        _type = type;
    }
}
