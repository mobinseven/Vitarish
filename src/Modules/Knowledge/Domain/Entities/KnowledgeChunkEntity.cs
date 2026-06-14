namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeChunkEntity : Entity
{
    public string ExternalId { get; set => SetField(ref field, value, nameof(ExternalId)); } = string.Empty;
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public Guid SourceEntityId { get; set => SetField(ref field, value, nameof(SourceEntityId)); }
    public Guid DocumentEntityId { get; set => SetField(ref field, value, nameof(DocumentEntityId)); }
    public string CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); } = string.Empty;
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public string SourceId { get; set => SetField(ref field, value, nameof(SourceId)); } = string.Empty;
    public string DocumentId { get; set => SetField(ref field, value, nameof(DocumentId)); } = string.Empty;
    public string Text { get; set => SetField(ref field, value, nameof(Text)); } = string.Empty;
    public int Order { get; set => SetField(ref field, value, nameof(Order)); }
    public string? Title { get; set => SetField(ref field, value, nameof(Title)); }
    public string? HeadingPath { get; set => SetField(ref field, value, nameof(HeadingPath)); }
}
