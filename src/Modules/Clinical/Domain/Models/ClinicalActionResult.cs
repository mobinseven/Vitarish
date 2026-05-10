namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalActionResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<WorkflowValidationResult> ValidationErrors { get; init; } = [];
    public IReadOnlyList<string> FhirRefsCreated { get; init; } = [];
    public IReadOnlyList<string> FhirRefsUpdated { get; init; } = [];
    public string? FhirTransactionReference { get; init; }
    public string? ErrorMessage { get; init; }
    public ClinicalActionRecordReadDto? ActionRecord { get; init; }

    public static ClinicalActionResult Success(
        IReadOnlyList<string> fhirRefsCreated,
        IReadOnlyList<string>? fhirRefsUpdated = null,
        string? fhirTransactionReference = null,
        ClinicalActionRecordReadDto? actionRecord = null) => new()
    {
        IsSuccess = true,
        FhirRefsCreated = fhirRefsCreated,
        FhirRefsUpdated = fhirRefsUpdated ?? [],
        FhirTransactionReference = fhirTransactionReference,
        ActionRecord = actionRecord
    };

    public static ClinicalActionResult Fail(
        IReadOnlyList<WorkflowValidationResult> validationErrors,
        string? errorMessage = null) => new()
    {
        IsSuccess = false,
        ValidationErrors = validationErrors,
        ErrorMessage = errorMessage
    };

    public static ClinicalActionResult Fail(string errorMessage) =>
        Fail([], errorMessage);
}
