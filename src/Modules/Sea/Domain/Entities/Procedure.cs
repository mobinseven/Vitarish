namespace Vitarish.Sea.Domain.Entities;

public class Procedure : Entity
{
    public Guid ProjectId { get; set; }
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        private set => SetField(ref _name, value, nameof(Name));
    }

    private string _metadata = "{}";
    public string Metadata
    {
        get => _metadata;
        private set => SetField(ref _metadata, value, nameof(Metadata));
    }

    public Procedure(Guid projectId, string name, string metadata = "{}")
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        _name = name;
        _metadata = metadata;
    }


    public ProcedureStats Stats { get; set; } = new();

    public void Rename(string name)
    {
        Name = name;
    }

    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }
}
