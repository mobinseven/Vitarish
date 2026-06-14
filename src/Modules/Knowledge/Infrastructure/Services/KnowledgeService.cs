using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services;

public sealed class KnowledgeService(IKnowledgeImporter importer, IKnowledgeIndexer indexer, IKnowledgeRetriever retriever, IKnowledgeAnswerer answerer, IKnowledgeEvaluator evaluator, IKnowledgeRepository repository) : IKnowledgeService
{
    public Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken) => repository.SaveCorpusAsync(corpus, cancellationToken);
    public Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => repository.GetCorpusAsync(corpusId, tenantId, cancellationToken);
    public Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken) => repository.ListCorporaAsync(tenantId, cancellationToken);
    public Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => repository.GetSourcesAsync(corpusId, tenantId, cancellationToken);
    public async Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
        await repository.DeleteSourceAsync(sourceId, corpusId, tenantId, cancellationToken);
        await indexer.DeleteFromIndexAsync(sourceId, corpusId, tenantId, cancellationToken);
    }
    public Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken) => importer.ImportAsync(request, cancellationToken);
    public Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken) => importer.ReplaceSourceAsync(sourceId, request, cancellationToken);
    public Task<KnowledgeIndexResult> IndexAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => indexer.IndexCorpusAsync(corpusId, tenantId, cancellationToken);
    public Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) => indexer.ReindexSourceAsync(sourceId, corpusId, tenantId, cancellationToken);
    public Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken) => retriever.RetrieveAsync(query, cancellationToken);
    public Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken) => answerer.AskAsync(request, cancellationToken);
    public Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken) => evaluator.EvaluateAsync(request, cancellationToken);
}
