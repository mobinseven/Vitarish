using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeEvaluator { Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken); }
