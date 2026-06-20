using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeAnswerCorrectnessScorer
{
    Task<KnowledgeAnswerCorrectnessScore> ScoreAsync(string expectedAnswer, string generatedAnswer, CancellationToken cancellationToken);
}
