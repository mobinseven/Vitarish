namespace Vitarish.Contents.Domain.Entities;

public class ContentRevision : Entity
{
    public Guid ContentId { get; set; }
    public int Version { get; set; }
    public string Snapshot { get; set; } = string.Empty;
}
