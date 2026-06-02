using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Records an eligibility inquiry made to a provider for a patient.
/// </summary>
public sealed class EligibilityInquiry
{
    public Guid Id { get; set; }
    public required string ProviderKey { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }
    public DateTimeOffset RequestTime { get; set; }
    public EligibilityStatus ResultStatus { get; set; }

    /// <summary>Provider-issued tracking code for this inquiry.</summary>
    public string? TrackingCode { get; set; }

    /// <summary>Provider-native status code preserved for audit.</summary>
    public string? RawStatusCode { get; set; }

    /// <summary>Provider-native status message preserved for audit.</summary>
    public string? RawStatusMessage { get; set; }

    /// <summary>When this eligibility result expires (provider-specific TTL).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
