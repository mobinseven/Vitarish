namespace Vitarish.Sea.Domain.Entities;

public class Procedure : Entity
{
    public Guid ProjectId { get; set; }
    public string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;
    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public ProcedureStats Stats { get; set; } = new();
}
