namespace Vitarish.Knowledge.Domain.Models;

[EntityDescriptorFor(typeof(KnowledgeCorpusEntity))]
public sealed record KnowledgeCorpusDescriptor : EntityDescriptor
{
    public string? TenantId { get; init; }
    public bool? IsActive { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeSourceEntity))]
public sealed record KnowledgeSourceDescriptor : EntityDescriptor
{
    public Guid? CorpusId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeDocumentEntity))]
public sealed record KnowledgeDocumentDescriptor : EntityDescriptor
{
    public Guid? CorpusId { get; init; }
    public Guid? SourceId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeChunkEntity))]
public sealed record KnowledgeChunkDescriptor : EntityDescriptor
{
    public IReadOnlyCollection<Guid>? CorpusIds { get; init; }
    public Guid? CorpusId { get; init; }
    public Guid? SourceId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeQuestionEntity))]
public sealed record KnowledgeQuestionDescriptor : EntityDescriptor
{
    public Guid? CorpusId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeAskTraceEntity))]
public sealed record KnowledgeAskTraceDescriptor : EntityDescriptor
{
    public Guid? CorpusId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeIndexRunEntity))]
public sealed record KnowledgeIndexRunDescriptor : EntityDescriptor
{
    public Guid? CorpusId { get; init; }
    public string? TenantId { get; init; }
}
