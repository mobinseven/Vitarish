using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Records a patient's insurance coverage with a specific provider.
/// </summary>
public sealed class PatientCoverage
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid InsuranceProviderId { get; set; }

    /// <summary>Provider-specific coverage/membership identifier.</summary>
    public string? CoverageIdentifier { get; set; }

    /// <summary>Insurance booklet/card number.</summary>
    public string? InsuranceBookletIdentifier { get; set; }

    /// <summary>Type of coverage (basic, complementary, etc.).</summary>
    public string? CoverageType { get; set; }

    public bool IsEligible { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Provider-native status code from last eligibility check.</summary>
    public string? RawStatusCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
