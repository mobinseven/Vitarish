namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeQuestionEntity : Entity
{
    public Guid CorpusEntityId { get; set => SetField(ref field, value, nameof(CorpusEntityId)); }
    public Guid CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); }
    public string Question { get; set => SetField(ref field, value, nameof(Question)); } = string.Empty;
    public string? ExpectedAnswer { get; set => SetField(ref field, value, nameof(ExpectedAnswer)); }
    public string ExpectedChunkIdsJson { get; set => SetField(ref field, value, nameof(ExpectedChunkIdsJson)); } = "[]";
}
