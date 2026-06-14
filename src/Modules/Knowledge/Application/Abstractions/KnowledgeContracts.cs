using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeChunker { IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document); }
public interface IKnowledgeImporter
{
    Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken);
    Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken);
}
public interface IKnowledgeIndexer
{
    Task<KnowledgeIndexResult> IndexCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteFromIndexAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
}
public interface IKnowledgeRetriever { Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken); }
public interface IKnowledgeContextBuilder { KnowledgeContext Build(string question, IReadOnlyList<KnowledgeHit> hits); }
public interface IKnowledgeAnswerer
{
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken);
}
public interface IKnowledgeEvaluator { Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken); }

public interface IKnowledgeCorpusRepository
{
    Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken);
    Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListAllActiveCorporaForIndexingAsync(CancellationToken cancellationToken);
}

public interface IKnowledgeSourceRepository
{
    Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
}

public interface IKnowledgeChunkRepository
{
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaForIndexingAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken);
}

public interface IKnowledgeQuestionRepository
{
    Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
}

public interface IKnowledgeAskTraceRepository
{
    Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken);
}

public interface IKnowledgeIndexRunRepository
{
    Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken);
    Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
}

public interface IKnowledgeImportRepository
{
    Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken);
    Task ReplaceSourceAsync(Guid sourceId, KnowledgeImportResult result, CancellationToken cancellationToken);
}

public interface IKnowledgeService : IKnowledgeImporter
{
    Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken);
    Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> IndexAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken);
}
