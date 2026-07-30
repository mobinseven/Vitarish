namespace Vitarish.Clinic.Contracts.PatientAgent;

public sealed record ExecutePatientAgentToolRequest(
    string ToolCallId,
    string ToolName,
    string ArgumentsJson,
    string ExecutionId,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record ExecutePatientAgentToolResponse(
    string CallId,
    string Output,
    bool IsError = false);
