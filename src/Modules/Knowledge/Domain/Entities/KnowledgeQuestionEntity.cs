namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeQuestionEntity : Entity
{
    public Guid CorpusId { get; set => SetField(ref field, value, nameof(CorpusId)); }
    public string Question { get; set => SetField(ref field, value, nameof(Question)); } = string.Empty;
    public string? ExpectedAnswer { get; set => SetField(ref field, value, nameof(ExpectedAnswer)); }
    public IReadOnlyList<Guid> ExpectedChunkIds { get; set => SetField(ref field, value, nameof(ExpectedChunkIds)); } = [];
}
