namespace Vitarish.Sea.Domain.Entities;

public class Role : Entity
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value, nameof(Name));
    }

    public Role(string name)
    {
        Id = Guid.NewGuid();
        _name = name;
    }
}
