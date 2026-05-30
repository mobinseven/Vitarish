namespace Vitarish.TaminInsurance.Client;

public sealed class TaminSession
{
    public required string Token { get; init; }
    public string? SessionId { get; init; }
}

public sealed class TaminAuthRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed class TaminAuthResponse
{
    public string? Token { get; init; }
    public string? SessionId { get; init; }
    public int? ExpiresInMinutes { get; init; }
}

public sealed class TaminEntitlementRequest
{
    public required string NationalCode { get; init; }
}

public sealed class TaminEntitlementResponse
{
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public string? TrackingCode { get; init; }
    public string? InsuranceNumber { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class TaminPrescriptionSubmitRequest
{
    public required string PatientId { get; init; }
    public required List<TaminPrescriptionItem> Items { get; init; }
}

public sealed class TaminPrescriptionItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public int Quantity { get; init; }
    public int? DurationDays { get; init; }
    public string? DosageInfo { get; init; }
}

public sealed class TaminSubmissionResponse
{
    public string? PrescriptionId { get; init; }
    public string? TrackingCode { get; init; }
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
}

public sealed class TaminFetchResponse
{
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public string? TrackingCode { get; init; }
}

public sealed class TaminReferenceDataResponse
{
    public string? Version { get; init; }
    public List<TaminReferenceItem>? Items { get; init; }
}

public sealed class TaminReferenceItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
}
