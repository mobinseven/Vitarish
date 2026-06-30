namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeUserWorkspaceReadModel(IReadOnlyList<KnowledgeUserCorpusReadModel> Corpora)
{
    public static KnowledgeUserWorkspaceReadModel Empty { get; } = new([]);
}

public sealed record KnowledgeUserCorpusReadModel(Guid CorpusId, string Name, string? Description, bool IsActive, int SourceCount, int ChunkCount);
