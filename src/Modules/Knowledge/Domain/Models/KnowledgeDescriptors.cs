namespace Vitarish.Knowledge.Domain.Models;

[EntityDescriptorFor(typeof(KnowledgeCorpusEntity))]
public sealed record KnowledgeCorpusDescriptor : EntityDescriptor
{
    public string? ExternalId { get; init; }
    public string? TenantId { get; init; }
    public bool? IsActive { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeSourceEntity))]
public sealed record KnowledgeSourceDescriptor : EntityDescriptor
{
    public string? ExternalId { get; init; }
    public string? CorpusId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeDocumentEntity))]
public sealed record KnowledgeDocumentDescriptor : EntityDescriptor
{
    public string? ExternalId { get; init; }
    public string? CorpusId { get; init; }
    public string? SourceId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeChunkEntity))]
public sealed record KnowledgeChunkDescriptor : EntityDescriptor
{
    public string? ExternalId { get; init; }
    public IReadOnlyCollection<string>? CorpusIds { get; init; }
    public string? CorpusId { get; init; }
    public string? SourceId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeQuestionEntity))]
public sealed record KnowledgeQuestionDescriptor : EntityDescriptor
{
    public string? CorpusId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeAskTraceEntity))]
public sealed record KnowledgeAskTraceDescriptor : EntityDescriptor
{
    public string? CorpusId { get; init; }
    public string? TenantId { get; init; }
}

[EntityDescriptorFor(typeof(KnowledgeIndexRunEntity))]
public sealed record KnowledgeIndexRunDescriptor : EntityDescriptor
{
    public string? CorpusId { get; init; }
    public string? TenantId { get; init; }
}
