namespace Vitarish.Knowledge.Application.Enums;

public enum KnowledgeEvaluationScope
{
    Full = 0,

    /// <summary>
    /// Randomly evaluates a subset of questions.
    /// Use RandomSeed for deterministic CI/regression runs.
    /// </summary>
    RandomSample = 1
}
