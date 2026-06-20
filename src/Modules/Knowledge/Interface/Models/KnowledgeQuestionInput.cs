namespace Vitarish.Knowledge.Interface.Models;

public sealed record KnowledgeQuestionInput(Guid? CorpusId, string Question, int TopK);
