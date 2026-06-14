namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeDocumentEntity : Entity
{
    public Guid CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); }
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public Guid SourceId { get; set => SetField(ref field, value, nameof(SourceId)); }
    public string Title { get; set => SetField(ref field, value, nameof(Title)); } = string.Empty;
    public string Text { get; set => SetField(ref field, value, nameof(Text)); } = string.Empty;
    public string? Product { get; set => SetField(ref field, value, nameof(Product)); }
    public string? Version { get; set => SetField(ref field, value, nameof(Version)); }
    public string? Language { get; set => SetField(ref field, value, nameof(Language)); }
}
