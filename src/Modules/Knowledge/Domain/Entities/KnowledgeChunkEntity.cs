namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeChunkEntity : Entity
{
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public Guid SourceEntityId { get; set => SetField(ref field, value, nameof(SourceEntityId)); }
    public Guid DocumentEntityId { get; set => SetField(ref field, value, nameof(DocumentEntityId)); }
    public Guid CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); }
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public Guid SourceId { get; set => SetField(ref field, value, nameof(SourceId)); }
    public Guid DocumentId { get; set => SetField(ref field, value, nameof(DocumentId)); }
    public string Text { get; set => SetField(ref field, value, nameof(Text)); } = string.Empty;
    public int Order { get; set => SetField(ref field, value, nameof(Order)); }
    public string? Title { get; set => SetField(ref field, value, nameof(Title)); }
    public string? HeadingPath { get; set => SetField(ref field, value, nameof(HeadingPath)); }
}
