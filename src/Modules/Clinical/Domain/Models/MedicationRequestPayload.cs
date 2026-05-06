namespace Vitarish.Clinical.Domain.Models;

public sealed record MedicationRequestPayload
{
    public required string MedicationCode { get; init; }
    public required string MedicationCodeSystem { get; init; }
    public string? MedicationDisplay { get; init; }
    public string? DosageInstruction { get; init; }
    public string? RouteCode { get; init; }
    public string? FrequencyCode { get; init; }
    public decimal? DoseQuantity { get; init; }
    public string? DoseUnit { get; init; }
    public int? DurationDays { get; init; }
    public string? Status { get; init; } = "active";
    public string? Intent { get; init; } = "order";
    public string? Notes { get; init; }
}
