using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeChunker { IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document); }
public interface IKnowledgeImporter { Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken); }
public interface IKnowledgeIndexer { Task<KnowledgeIndexResult> IndexCorpusAsync(string corpusId, CancellationToken cancellationToken); }
public interface IKnowledgeRetriever { Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken); }
public interface IKnowledgeContextBuilder { KnowledgeContext Build(string question, IReadOnlyList<KnowledgeHit> hits); }
public interface IKnowledgeAnswerer { Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken); }
public interface IKnowledgeEvaluator { Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken); }

public interface IKnowledgeRepository
{
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(string corpusId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(string corpusId, CancellationToken cancellationToken);
    Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken);
    Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken);
}

public interface IKnowledgeService : IKnowledgeImporter
{
    Task<KnowledgeIndexResult> IndexAsync(string corpusId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken);
}
