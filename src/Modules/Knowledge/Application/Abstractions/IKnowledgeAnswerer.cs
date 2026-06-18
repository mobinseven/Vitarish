using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeAnswerer
{
    Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken);
}
