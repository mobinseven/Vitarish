using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeQuestionRepository
{
    Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken = default);
}
