using MediatR;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Queries;

/// <summary>Gets a prescription by ID with its items.</summary>
public sealed record GetPrescriptionQuery : IRequest<PrescriptionDto?>
{
    public required Guid PrescriptionId { get; init; }
}

/// <summary>Gets prescription history for a patient.</summary>
public sealed record GetPrescriptionHistoryQuery : IRequest<IReadOnlyList<PrescriptionSummaryDto>>
{
    public required Guid PatientId { get; init; }
    public Guid? ClinicId { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}

/// <summary>Checks patient eligibility with the selected provider.</summary>
public sealed record GetEligibilityQuery : IRequest<EligibilityResultDto>
{
    public required string PatientNationalId { get; init; }
    public required string ProviderKey { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
}

/// <summary>Searches reference codes for a provider.</summary>
public sealed record SearchReferenceCodesQuery : IRequest<IReadOnlyList<ReferenceCodeDto>>
{
    public required string ProviderKey { get; init; }
    public required string CodeSystem { get; init; }
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public int Take { get; init; } = 50;
}

// DTOs

public sealed record PrescriptionDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid ClinicId { get; init; }
    public string? SelectedProviderKey { get; init; }
    public PrescriptionStatus Status { get; init; }
    public int VersionNumber { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
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
    public PrescriptionStatus Status { get; init; }
    public string? SelectedProviderKey { get; init; }
    public int ItemCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}

public sealed record EligibilityResultDto
{
    public bool IsSuccess { get; init; }
    public EligibilityStatus Status { get; init; }
    public string? TrackingCode { get; init; }
    public string? CoverageIdentifier { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record ReferenceCodeDto
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string CodeSystem { get; init; } = string.Empty;
}
