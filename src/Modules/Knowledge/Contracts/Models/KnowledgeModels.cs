using Vitarish.Knowledge.Contracts.Enums;

namespace Vitarish.Knowledge.Contracts.Models;

public sealed record KnowledgeOwnerCorpusOption(string Label, string Details) { public Guid CorpusId { get; init; } }
public sealed record KnowledgeOwnerSourceOption(string Label) { public Guid SourceId { get; init; } }
public sealed record KnowledgeOwnerWorkspaceModel(IReadOnlyList<KnowledgeOwnerCorpusOption> Corpora, KnowledgeOwnerCorpusOption? SelectedCorpus, IReadOnlyList<KnowledgeOwnerSourceOption> Sources) { public static KnowledgeOwnerWorkspaceModel Empty { get; } = new([], null, []); }
public sealed record KnowledgeOwnerCollectionModel(IReadOnlyList<KnowledgeOwnerCorpusCollectionItem> Corpora, Guid? SelectedCorpusId, Guid? FocusedSourceId) { public static KnowledgeOwnerCollectionModel Empty { get; } = new([], null, null); }
public sealed record KnowledgeOwnerCorpusCollectionItem(Guid CorpusId, string CorpusName, string? CorpusDescription, bool IsActive, IReadOnlyList<KnowledgeOwnerSourceCollectionItem> Sources);
public sealed record KnowledgeOwnerSourceCollectionItem(Guid SourceId, string SourceName, string SourceType, DateTimeOffset ImportedAt, string? OriginalLocation, IReadOnlyList<KnowledgeOwnerSourceFileCollectionItem> Files);
public sealed record KnowledgeOwnerSourceFileCollectionItem(Guid FileId, string FileName, string? ContentType, long? Size, int ChunkCount, DateTimeOffset? IndexedAt);
public sealed record KnowledgeCitation(Guid ChunkId, Guid SourceId, Guid DocumentId, string? Title, string? Quote);
public sealed record KnowledgeRetrievalHit(Guid ChunkId, Guid SourceId, Guid DocumentId, string Text, string? Title, double Score);
public sealed record KnowledgeUserWorkspaceModel(IReadOnlyList<KnowledgeUserCorpusModel> Corpora);
public sealed record KnowledgeUserCorpusModel(Guid CorpusId, string Name, string? Description, bool IsActive, int SourceCount, int ChunkCount);
public sealed record KnowledgeAnswerModel(string Answer, bool IsAnswered, IReadOnlyList<KnowledgeCitation> Citations, string? MissingInformation, IReadOnlyList<KnowledgeRetrievalHit> RetrievedContext);
public sealed record KnowledgeCheckModel(bool Passed, string Message, IReadOnlyDictionary<string, object?> Data);
public sealed record KnowledgeCorpusDiagnosticsModel(Guid CorpusId, string TenantId, string Name, bool IsActive, int SourceCount, int DocumentCount, int ChunkCount, int QuestionCount, int AskTraceCount, DateTimeOffset? LastImportedAt, DateTimeOffset? LastAskedAt);
public sealed record KnowledgeSourceDiagnosticsModel(Guid SourceId, Guid CorpusId, string TenantId, string Name, string SourceType, string? OriginalLocation, string? Version, DateTimeOffset ImportedAt);
public sealed record KnowledgeChunkDiagnosticsModel(Guid ChunkId, Guid SourceId, Guid DocumentId, string Title, string? HeadingPath, int Order, int TextLength, string TextPreview, string Text);
public sealed record KnowledgeAskTraceDiagnosticsModel(Guid TraceId, Guid CorpusId, string TenantId, string Question, bool IsAnswered, string? MissingInformation, IReadOnlyList<Guid> RetrievedChunkIds, string? RetrievedChunkIdsParseError, string GeneratedAnswer, IReadOnlyList<KnowledgeCitation> Citations, string? CitationsParseError, string? TraceKind, DateTimeOffset CreatedAt);
public sealed record KnowledgeAskDebugModel(Guid CorpusId, string TenantId, string Question, IReadOnlyList<KnowledgeRetrievalHit> RetrievedHits, IReadOnlyList<Guid> ContextChunkIds, KnowledgeAnswerModel Answer, KnowledgeFailureStage FailureStage, string? FailureReason);
public sealed record KnowledgePipelineDiagnosticsModel(KnowledgeCheckModel CorpusCreated, KnowledgeCheckModel ContentImported, KnowledgeCheckModel ChunksCreated, KnowledgeCheckModel CorpusIndexed, KnowledgeCheckModel SearchRetrievedChunks, KnowledgeCheckModel ContextPackedChunks, KnowledgeCheckModel AnswerHasValidCitations, KnowledgeCheckModel RefusalIsGrounded, KnowledgeAskDebugModel? DebugAsk);
public sealed record KnowledgeIndexDiagnosticsModel(Guid CorpusId, string TenantId, int StoredChunkCount, int SearchHitCountForProbe, DateTimeOffset? LastIndexedAt, bool LooksIndexed);
public sealed record KnowledgeEvaluationItemModel(Guid QuestionId, bool HasExpectedContext, bool ExpectedContextFoundInTop5, bool ExpectedContextFoundInTop10, bool ExpectedContextWasRetrieved, bool WasAnswered, bool ShouldHaveAnswered, bool? GeneratedAnswerMatchedExpectedAnswer, string? GeneratedAnswer);
public sealed record KnowledgeEvaluationResultModel(Guid Id, Guid CorpusId, string TenantId, IReadOnlyList<KnowledgeEvaluationItemModel> Items, double RecallAt5, double RecallAt10, double AverageExpectedChunkRank);
