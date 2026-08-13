namespace Vitarish.Clinic.Contracts.ClinicAssistant;

public sealed record ExecuteClinicAssistantToolRequest(
    string ToolCallId,
    string ToolName,
    string ArgumentsJson,
    string ExecutionId,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record ExecuteClinicAssistantToolResponse(
    string CallId,
    string Output,
    bool IsError = false);
