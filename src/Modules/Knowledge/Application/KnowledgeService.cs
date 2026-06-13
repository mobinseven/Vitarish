using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application;

public sealed class KnowledgeService(IKnowledgeImporter importer, IKnowledgeIndexer indexer, IKnowledgeRetriever retriever, IKnowledgeAnswerer answerer, IKnowledgeEvaluator evaluator) : IKnowledgeService
{
    public Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken) => importer.ImportAsync(request, cancellationToken);
    public Task<KnowledgeIndexResult> IndexAsync(string corpusId, CancellationToken cancellationToken) => indexer.IndexCorpusAsync(corpusId, cancellationToken);
    public Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken) => retriever.RetrieveAsync(query, cancellationToken);
    public Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken) => answerer.AskAsync(request, cancellationToken);
    public Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken) => evaluator.EvaluateAsync(request, cancellationToken);
}
