using System.Text.Json;

namespace Vitarish.Intelligence.Contracts;

public enum IntelligenceErrorCode { ValidationFailure, Unauthorized, Forbidden, UnsupportedMediaType, FileUnavailable, TooLargePayload, ProviderUnavailable, ProviderTimeout, PartialStreamFailure, MalformedChunk, Cancellation, DeserializationFailure, UnexpectedFailure, NotFound, Conflict, InvalidHistory }
public enum ResponderInputType { Text, Image, File, McpToolCallApproval }
public enum ResponderOutputType { Text, McpToolCall, McpListTools, McpApprovalRequest, Message, Reasoning, Completion, Json, FileReference }
public enum StreamChunkKind { Started, Progress, Delta, Completed, Failed, Canceled }
public enum IntelligenceProgressStage { Queued, Validating, FileInputAccepted, ProviderSelected, Transcribing, Responding, Rewriting, Completed, Failed, Canceled }
public enum ResponderInputImageDetail { Low, High, Auto }
public enum ResponseTextFormat { Text, JsonObject, JsonSchema }
public enum ResponderToolApprovalRequirement { Never, Always }

public sealed record IntelligenceError(IntelligenceErrorCode Code, string Message, int? StatusCode = null, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
public sealed record IntelligenceResult<T>(T? Value, IntelligenceError? Error) { public bool Succeeded => Error is null; public static IntelligenceResult<T> Success(T value) => new(value, null); public static IntelligenceResult<T> Failed(IntelligenceError error) => new(default, error); }
public sealed record IntelligenceFileReference { public string? FileId { get; init; } public string? FileName { get; init; } public string? ContentType { get; init; } public long? Size { get; init; } public string? Checksum { get; init; } public string? TemporaryUploadReference { get; init; } public string? PreviewReference { get; init; } }
public record ProviderSafeOptions { public string? Provider { get; init; } public string? ModelId { get; init; } public string? Profile { get; init; } public double? Temperature { get; init; } public int? MaxOutputTokens { get; init; } }
public sealed record IntelligenceSessionContext { public string? SessionId { get; init; } public string? ConversationId { get; init; } public string? OperationId { get; init; } }

public sealed record ResponderInput(ResponderInputType Type = ResponderInputType.Text, string? Text = null, string? ImageUrl = null, ResponderInputImageDetail? Detail = null, string? FileId = null, string? FileData = null, string? Filename = null, string? ApprovalId = null, IntelligenceFileReference? File = null)
{
    public static ResponderInput TextInput(string text) => new(ResponderInputType.Text, Text: text);
    public static ResponderInput ImageInput(string imageUrl, ResponderInputImageDetail? detail = null) => new(ResponderInputType.Image, ImageUrl: imageUrl, Detail: detail);
    public static ResponderInput FileInput(string? fileId = null, string? fileData = null, string? filename = null) => new(ResponderInputType.File, FileId: fileId, FileData: fileData, Filename: filename, File: new IntelligenceFileReference { FileId = fileId, TemporaryUploadReference = fileData, FileName = filename });
    public static ResponderInput McpToolCallApprovalInput(string approvalId) => new(ResponderInputType.McpToolCallApproval, ApprovalId: approvalId);
}
public sealed record ResponderToolDefinition { public required string Type { get; init; } public string? ServerLabel { get; init; } public string? ServerUrl { get; init; } public string? ServerToken { get; init; } public IReadOnlyList<string> AllowedTools { get; init; } = []; public ResponderToolApprovalRequirement RequireApproval { get; init; } = ResponderToolApprovalRequirement.Never; }
public sealed record ResponderOptions : ProviderSafeOptions { public string? Instructions { get; init; } public ResponseTextFormat TextFormat { get; init; } = ResponseTextFormat.Text; public string? JsonSchema { get; init; } public string? PreviousResponseId { get; init; } public string? Model { get; init; } public IReadOnlyList<ResponderToolDefinition> Tools { get; init; } = []; }
public sealed record TranscriberOptions : ProviderSafeOptions { public string? Language { get; init; } public string ContentType { get; init; } = "audio/webm"; }
public sealed record RewriteOptions : ProviderSafeOptions { public string? Instructions { get; init; } }
public sealed record McpToolDescription(string Name, string? Description);
public sealed record ResponderOutput(ResponderOutputType Type = ResponderOutputType.Text, string? Text = null, string? ToolName = null, string? ToolArguments = null, string? ServerLabel = null, string? Id = null, IReadOnlyList<McpToolDescription>? Tools = null, IReadOnlyList<ResponderOutput>? Content = null, IReadOnlyDictionary<string, string>? Metadata = null, IntelligenceFileReference? File = null);

public sealed record ResponderRequest(IReadOnlyList<ResponderInput> Inputs, ResponderOptions? Options = null, IntelligenceSessionContext? Context = null);
public sealed record ResponderResult(IReadOnlyList<ResponderOutput> Outputs, IntelligenceSessionContext? Context = null);
public sealed record RunAgentConversationRequest(
    string AgentKey,
    string SubjectKind,
    string SubjectId,
    string Message,
    string? ConversationId = null,
    string? PreviousResponseId = null);
public sealed record RunAgentConversationResponse(
    string AgentRunId,
    string ConversationId,
    JsonElement Result);
public sealed record RewriteRequest(string Prompt, RewriteOptions? Options = null, IntelligenceSessionContext? Context = null);
public sealed record RewriteResult(string Text, IntelligenceSessionContext? Context = null);
public sealed record TranscriptionRequest(IntelligenceFileReference AudioFile, TranscriberOptions? Options = null, IntelligenceSessionContext? Context = null);
public sealed record TranscriptionResult(string Text, IntelligenceSessionContext? Context = null);
public sealed record IntelligenceStreamMetadata(string OperationId, long Sequence, DateTimeOffset Timestamp, IntelligenceProgressStage Stage, IntelligenceSessionContext? Context = null);
public sealed record IntelligenceStreamChunk<T>(StreamChunkKind Kind, IntelligenceStreamMetadata Metadata, T? Delta = default, IntelligenceError? Error = null);
public sealed record TranscriptionProgress(IntelligenceProgressStage Stage, string? Text = null, int? Percent = null);
public sealed record ResponderProgress(IntelligenceProgressStage Stage, ResponderOutput? Output = null, int? Percent = null);
public sealed record RewriteProgress(IntelligenceProgressStage Stage, string? Text = null, int? Percent = null);
public sealed record FileInputProgress(IntelligenceProgressStage Stage, IntelligenceFileReference File, int? Percent = null);
