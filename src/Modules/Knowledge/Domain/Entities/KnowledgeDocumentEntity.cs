namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeDocumentEntity : Entity
{
    public string ExternalId { get; set => SetField(ref field, value, nameof(ExternalId)); } = string.Empty;
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public Guid SourceEntityId { get; set => SetField(ref field, value, nameof(SourceEntityId)); }
    public string CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); } = string.Empty;
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public string SourceId { get; set => SetField(ref field, value, nameof(SourceId)); } = string.Empty;
    public string Title { get; set => SetField(ref field, value, nameof(Title)); } = string.Empty;
    public string Text { get; set => SetField(ref field, value, nameof(Text)); } = string.Empty;
    public string? Product { get; set => SetField(ref field, value, nameof(Product)); }
    public string? Version { get; set => SetField(ref field, value, nameof(Version)); }
    public string? Language { get; set => SetField(ref field, value, nameof(Language)); }
}
