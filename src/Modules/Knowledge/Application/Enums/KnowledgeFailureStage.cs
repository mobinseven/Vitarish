namespace Vitarish.Knowledge.Application.Enums;

public enum KnowledgeFailureStage
{
    None,
    CorpusMissing,
    NoImportedChunks,
    Retrieval,
    ContextPacking,
    Generation,
    CitationValidation,
    InsufficientContext
}
