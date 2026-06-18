using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Infrastructure.Services.Diagnostics;

public static class KnowledgeFailureClassifier
{
    public static KnowledgeFailureStage Classify(int corpusChunkCount, IReadOnlyList<KnowledgeHit> hits, KnowledgeContext context, KnowledgeAnswer answer)
    {
        if (corpusChunkCount == 0) return KnowledgeFailureStage.NoImportedChunks;
        if (hits.Count == 0) return KnowledgeFailureStage.Retrieval;
        if (context.ChunkIds.Count == 0) return KnowledgeFailureStage.ContextPacking;
        if (!answer.IsAnswered && answer.MissingInformation?.Contains("cited context chunks that were not retrieved", StringComparison.OrdinalIgnoreCase) == true) return KnowledgeFailureStage.CitationValidation;
        if (!answer.IsAnswered) return KnowledgeFailureStage.InsufficientContext;
        return KnowledgeFailureStage.None;
    }
}
