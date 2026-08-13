namespace Vitarish.Clinic.Contracts.ClinicAssistant;

public sealed record ExecuteClinicAssistantToolResponse(
    string CallId,
    string Output,
    bool IsError = false);
