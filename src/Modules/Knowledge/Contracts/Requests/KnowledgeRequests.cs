using Vitarish.Knowledge.Contracts.Enums;

namespace Vitarish.Knowledge.Contracts.Requests;

public sealed record CreateKnowledgeCorpusRequest(string Name, string? Description, bool IsActive);
public sealed record ImportKnowledgeSourceRequest(Guid? CorpusId, string? SourceName, KnowledgeSourceInputFormat Format, string Content);
public sealed record ImportKnowledgeSourceFromFileRequest(Guid? CorpusId, Guid? FileInformationId, string? SourceName, KnowledgeSourceInputFormat Format);
public sealed record ReplaceKnowledgeSourceRequest(Guid? CorpusId, Guid? SourceId, string? SourceName, KnowledgeSourceInputFormat Format, string Content);
public sealed record ReplaceKnowledgeSourceFromFileRequest(Guid? CorpusId, Guid? SourceId, Guid? FileInformationId, string? SourceName, KnowledgeSourceInputFormat Format);
public sealed record DeleteKnowledgeSourceRequest(Guid? CorpusId, Guid? SourceId);
public sealed record IndexKnowledgeCorpusRequest(Guid? CorpusId);
public sealed record AskKnowledgeRequest(Guid CorpusId, string Question, int TopK = 8, KnowledgeRetrievalMode RetrievalMode = KnowledgeRetrievalMode.Balanced, bool SaveTrace = true, string? TraceKind = null);
public sealed record EvaluateKnowledgeCorpusRequest(Guid? CorpusId, KnowledgeEvaluationScope EvaluationScope = KnowledgeEvaluationScope.Full);
public sealed record KnowledgeOwnerWorkspaceRequest(Guid? SelectedCorpusId = null, Guid? FocusedSourceId = null);
public sealed record KnowledgeDiagnosticsRequest(Guid CorpusId, string? Question = null, int TopK = 8);
public sealed record KnowledgeChunkDiagnosticsRequest(Guid CorpusId, Guid? SourceId = null, Guid? DocumentId = null, string? HeadingContains = null, string? TextContains = null, int Limit = 100);
public sealed record KnowledgeAskTraceDiagnosticsRequest(Guid CorpusId, bool? IsAnswered = null, string? TraceKind = null, string? QuestionContains = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int Limit = 100);
public sealed record KnowledgeCorpusDiagnosticsRequest(Guid CorpusId);
