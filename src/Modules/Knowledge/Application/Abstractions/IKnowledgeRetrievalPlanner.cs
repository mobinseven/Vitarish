using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeRetrievalPlanner
{
    Task<KnowledgeRetrievalPlan> PlanAsync(string originalQuestion, CancellationToken cancellationToken);
}
