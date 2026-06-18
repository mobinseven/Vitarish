namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgePipelineDiagnostics
{
    public required KnowledgeCheck CorpusCreated { get; init; }
    public required KnowledgeCheck ContentImported { get; init; }
    public required KnowledgeCheck ChunksCreated { get; init; }
    public required KnowledgeCheck CorpusIndexed { get; init; }
    public required KnowledgeCheck SearchRetrievedChunks { get; init; }
    public required KnowledgeCheck ContextPackedChunks { get; init; }
    public required KnowledgeCheck AnswerHasValidCitations { get; init; }
    public required KnowledgeCheck RefusalIsGrounded { get; init; }
    public KnowledgeAskDebugResult? DebugAsk { get; init; }
}
