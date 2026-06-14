namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeIndexRunEntity : Entity
{
    public string ExternalId { get; set => SetField(ref field, value, nameof(ExternalId)); } = string.Empty;
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public string CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); } = string.Empty;
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public int IndexedChunkCount { get; set => SetField(ref field, value, nameof(IndexedChunkCount)); }
    public string Scope { get; set => SetField(ref field, value, nameof(Scope)); } = "corpus";
    public string? SourceId { get; set => SetField(ref field, value, nameof(SourceId)); }
    public DateTimeOffset CreatedAt { get; set => SetField(ref field, value, nameof(CreatedAt)); }
}
