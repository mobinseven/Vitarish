namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeAskTraceEntity : Entity
{
    public string ExternalId { get; set => SetField(ref field, value, nameof(ExternalId)); } = string.Empty;
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public string CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); } = string.Empty;
    public string Question { get; set => SetField(ref field, value, nameof(Question)); } = string.Empty;
    public string TenantId { get; set => SetField(ref field, value, nameof(TenantId)); } = string.Empty;
    public string RetrievedChunkIdsJson { get; set => SetField(ref field, value, nameof(RetrievedChunkIdsJson)); } = "[]";
    public string GeneratedAnswer { get; set => SetField(ref field, value, nameof(GeneratedAnswer)); } = string.Empty;
    public string? MissingInformation { get; set => SetField(ref field, value, nameof(MissingInformation)); }
    public string CitationsJson { get; set => SetField(ref field, value, nameof(CitationsJson)); } = "[]";
    public string? TraceKind { get; set => SetField(ref field, value, nameof(TraceKind)); }
    public bool IsAnswered { get; set => SetField(ref field, value, nameof(IsAnswered)); }
    public DateTimeOffset CreatedAt { get; set => SetField(ref field, value, nameof(CreatedAt)); }
}
