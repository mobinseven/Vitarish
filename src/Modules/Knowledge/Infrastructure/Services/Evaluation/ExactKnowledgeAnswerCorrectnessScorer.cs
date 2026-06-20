using System.Text.RegularExpressions;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Infrastructure.Services.Evaluation;

public sealed class ExactKnowledgeAnswerCorrectnessScorer : IKnowledgeAnswerCorrectnessScorer
{
    public Task<KnowledgeAnswerCorrectnessScore> ScoreAsync(string expectedAnswer, string generatedAnswer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedAnswer);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatedAnswer);

        bool matched = NormalizeAnswer(expectedAnswer) == NormalizeAnswer(generatedAnswer);
        string rationale = matched
            ? "Normalized generated answer exactly matched the expected answer."
            : "Normalized generated answer did not exactly match the expected answer.";

        return Task.FromResult(new KnowledgeAnswerCorrectnessScore(matched, rationale));
    }

    private static string NormalizeAnswer(string answer) => Regex.Replace(answer.Trim().ToLowerInvariant(), @"\s+", " ");
}
