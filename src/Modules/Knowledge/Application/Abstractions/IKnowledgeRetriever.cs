using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeRetriever { Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken); }
