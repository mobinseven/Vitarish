namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeSourceEntity : Entity
{
    public Guid CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); }
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public string Name { get; set => SetField(ref field, value, nameof(Name)); } = string.Empty;
    public string SourceType { get; set => SetField(ref field, value, nameof(SourceType)); } = string.Empty;
    public string? OriginalLocation { get; set => SetField(ref field, value, nameof(OriginalLocation)); }
    public string? Version { get; set => SetField(ref field, value, nameof(Version)); }
    public string? Checksum { get; set => SetField(ref field, value, nameof(Checksum)); }
    public DateTimeOffset ImportedAt { get; set => SetField(ref field, value, nameof(ImportedAt)); }
}
