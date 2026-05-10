namespace Vitarish.Clinical.Domain.Models;

public sealed record DiagnosisPayload
{
    public required string Code { get; init; }
    public required string CodeSystem { get; init; }
    public string? DisplayText { get; init; }
    public string? ClinicalStatus { get; init; } = "active";
    public string? VerificationStatus { get; init; } = "confirmed";
    public string? Notes { get; init; }
}
