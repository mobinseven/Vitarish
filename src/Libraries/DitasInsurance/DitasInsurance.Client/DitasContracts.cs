namespace Vitarish.DitasInsurance.Client;

public sealed record DitasEntitlementResponse
{
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
    public string? MembershipNumber { get; init; }
    public string? TrackingCode { get; init; }
    public DateTimeOffset? ValidUntil { get; init; }
}

public sealed record DitasPrescriptionSubmitRequest
{
    public string? PrescriptionId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record DitasSubmissionResponse
{
    public string? PrescriptionNumber { get; init; }
    public string? TrackingCode { get; init; }
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
}

public sealed record DitasFetchResponse
{
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
    public string? TrackingCode { get; init; }
}
