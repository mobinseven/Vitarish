using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeChunker { IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document); }
public interface IKnowledgeImporter
{
    Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken);
    Task<KnowledgeImportResult> ReplaceSourceAsync(string sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken);
}
public interface IKnowledgeIndexer
{
    Task<KnowledgeIndexResult> IndexCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> ReindexSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
}
public interface IKnowledgeRetriever { Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken); }
public interface IKnowledgeContextBuilder { KnowledgeContext Build(string question, IReadOnlyList<KnowledgeHit> hits); }
public interface IKnowledgeAnswerer
{
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken);
}
public interface IKnowledgeEvaluator { Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken); }

public interface IKnowledgeRepository
{
    Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken);
    Task<KnowledgeCorpus?> GetCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<bool> SourceExistsAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<string> corpusIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken);
    Task DeleteSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
    Task ReplaceSourceAsync(string sourceId, KnowledgeImportResult result, CancellationToken cancellationToken);
    Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken);
    Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken);
    Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
}

public interface IKnowledgeService : IKnowledgeImporter
{
    Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken);
    Task<KnowledgeCorpus?> GetCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> IndexAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> ReindexSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken);
}
