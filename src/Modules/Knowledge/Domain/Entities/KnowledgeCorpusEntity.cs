namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeCorpusEntity : Entity
{
    public string Name { get; set => SetField(ref field, value, nameof(Name)); } = string.Empty;
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public string? Description { get; set => SetField(ref field, value, nameof(Description)); }
    public bool IsActive { get; set => SetField(ref field, value, nameof(IsActive)); }
    public DateTimeOffset CreatedAt { get; set => SetField(ref field, value, nameof(CreatedAt)); }
}
