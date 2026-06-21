using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record EvaluateKnowledgeCorpusInput(Guid? CorpusId, KnowledgeEvaluationScope EvaluationScope);
