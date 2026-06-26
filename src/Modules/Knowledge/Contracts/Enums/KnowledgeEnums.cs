namespace Vitarish.Knowledge.Contracts.Enums;

public enum KnowledgeSourceInputFormat { Markdown = 0, PlainText = 1, TechQaJson = 2, RagEvaluationJson = 3, PlainQaJson = 4 }
public enum KnowledgeEvaluationScope { Full = 0, RandomSample = 1 }
public enum KnowledgeFailureStage { None, CorpusMissing, NoImportedChunks, Retrieval, ContextPacking, Generation, CitationValidation, InsufficientContext }
public enum KnowledgeRetrievalMode { Balanced, Broad }
