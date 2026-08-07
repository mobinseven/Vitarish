using MediatR;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.Queries.Prescriptions;

/// <summary>Gets a prescription by ID with its items.</summary>
public sealed record GetPrescriptionQuery : IRequest<PrescriptionDto?>
{
    public required Guid PrescriptionId { get; init; }
}

/// <summary>Gets prescription history for a patient.</summary>
public sealed record GetPrescriptionHistoryQuery : IRequest<IReadOnlyList<PrescriptionSummaryDto>>
{
    public Guid? PatientId { get; init; }
    public Guid? EncounterId { get; init; }
    public Guid? ClinicId { get; init; }
    public PrescriptionStatus? StatusFilter { get; init; }
    public string? ProviderKeyFilter { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}
public sealed record GetPatientPrescriptionDashboardQuery : IRequest<PatientPrescriptionDashboardDto> { public required Guid PatientId { get; init; } public Guid? ClinicId { get; init; } public int AttentionTake { get; init; } = 8; }
public sealed record PatientPrescriptionDashboardDto { public int TotalCount { get; init; } public int AttentionCount { get; init; } public IReadOnlyList<PrescriptionSummaryDto> AttentionQueue { get; init; } = []; }

/// <summary>Checks patient eligibility with the selected provider.</summary>
public sealed record GetEligibilityQuery : IRequest<EligibilityResultDto>
{
    public required string PatientNationalId { get; init; }
    public required string ProviderKey { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
}

/// <summary>Gets the list of insurance providers available via API adapters (for payer selection).</summary>
public sealed record GetAvailableProvidersQuery : IRequest<IReadOnlyList<AvailableProviderDto>>;

// DTOs

public sealed record PrescriptionDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string? PatientNationalId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid ClinicId { get; init; }
    public Guid? EncounterId { get; init; }
    public string? SelectedProviderKey { get; init; }
    public PrescriptionStatus Status { get; init; }
    public int VersionNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? TrackingCode { get; init; }
    public IReadOnlyList<PrescriptionItemDto> Items { get; init; } = [];
}

public sealed record PrescriptionItemDto
{
    public Guid Id { get; init; }
    public PrescriptionItemCategory Category { get; init; }
    public string? ReferenceCode { get; init; }
    public string? DisplayName { get; init; }
    public string? DosageJson { get; init; }
    public int Quantity { get; init; }
    public int? DurationDays { get; init; }
    public int SortOrder { get; init; }
}

public sealed record PrescriptionSummaryDto
{
    public Guid Id { get; init; }
    public Guid? EncounterId { get; init; }
    public PrescriptionStatus Status { get; init; }
    public string? SelectedProviderKey { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? TrackingCode { get; init; }
    public IReadOnlyList<string> ItemDisplayNames { get; init; } = [];
}

public sealed record EligibilityResultDto
{
    public bool IsSuccess { get; init; }
    public EligibilityStatus Status { get; init; }
    public string? TrackingCode { get; init; }
    public string? CoverageIdentifier { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed record AvailableProviderDto
{
    public string ProviderKey { get; init; } = string.Empty;
}
