using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed class KnowledgeEvaluationRequest
{
    public Guid CorpusId { get; init; }

    public required string TenantId { get; init; }

    public int TopK { get; init; } = 10;

    /// <summary>
    /// Full is the safest default because it gives stable benchmark/regression results.
    /// Use RandomSample for local smoke tests or faster CI runs.
    /// </summary>
    public KnowledgeEvaluationScope Scope { get; init; } = KnowledgeEvaluationScope.Full;

    /// <summary>
    /// Used only when Scope is RandomSample.
    /// Defaults to 10.
    /// </summary>
    public int SampleSize { get; init; } = 10;

    /// <summary>
    /// Optional deterministic seed.
    /// Use this in CI so the same sample is evaluated across runs.
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// False = evaluate retrieval only.
    /// True = evaluate retrieval + generated answer behavior.
    /// </summary>
    public bool EvaluateAnswers { get; init; } = true;
}
