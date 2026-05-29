using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Commands;

/// <summary>Creates a new draft prescription.</summary>
public sealed record CreateDraftCommand : IRequest<CreateDraftResult>
{
    public required Guid PatientId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
    public Guid? EncounterId { get; init; }
    public string? SelectedProviderKey { get; init; }
    public Guid? SelectedPatientCoverageId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record CreateDraftResult
{
    public bool IsSuccess { get; init; }
    public Guid? PrescriptionId { get; init; }
    public string? ErrorMessage { get; init; }

    public static CreateDraftResult Success(Guid id) => new() { IsSuccess = true, PrescriptionId = id };
    public static CreateDraftResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Adds an item to a draft prescription.</summary>
public sealed record AddItemCommand : IRequest<AddItemResult>
{
    public required Guid PrescriptionId { get; init; }
    public required PrescriptionItemCategory Category { get; init; }
    public string? ReferenceCode { get; init; }
    public string? DisplayName { get; init; }
    public string? DosageJson { get; init; }
    public int Quantity { get; init; } = 1;
    public int? DurationDays { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed record AddItemResult
{
    public bool IsSuccess { get; init; }
    public Guid? ItemId { get; init; }
    public string? ErrorMessage { get; init; }

    public static AddItemResult Success(Guid id) => new() { IsSuccess = true, ItemId = id };
    public static AddItemResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Removes an item from a draft prescription.</summary>
public sealed record RemoveItemCommand : IRequest<RemoveItemResult>
{
    public required Guid PrescriptionId { get; init; }
    public required Guid ItemId { get; init; }
}

public sealed record RemoveItemResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RemoveItemResult Success() => new() { IsSuccess = true };
    public static RemoveItemResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Validates and marks a draft as ready for submission.</summary>
public sealed record ValidateDraftCommand : IRequest<ValidateDraftResult>
{
    public required Guid PrescriptionId { get; init; }
}

public sealed record ValidateDraftResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>Submits a prescription to the selected provider.</summary>
public sealed record SubmitPrescriptionCommand : IRequest<SubmitPrescriptionResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record SubmitPrescriptionResult
{
    public bool IsSuccess { get; init; }
    public string? RemotePrescriptionId { get; init; }
    public string? TrackingCode { get; init; }
    public PrescriptionStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static SubmitPrescriptionResult Success(string? remoteId, string? trackingCode, PrescriptionStatus status)
        => new() { IsSuccess = true, RemotePrescriptionId = remoteId, TrackingCode = trackingCode, ResultStatus = status };
    public static SubmitPrescriptionResult Failure(string error, PrescriptionStatus status)
        => new() { IsSuccess = false, ErrorMessage = error, ResultStatus = status };
}

/// <summary>Edits a previously accepted prescription.</summary>
public sealed record EditPrescriptionCommand : IRequest<EditPrescriptionResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record EditPrescriptionResult
{
    public bool IsSuccess { get; init; }
    public string? TrackingCode { get; init; }
    public PrescriptionStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static EditPrescriptionResult Success(string? trackingCode, PrescriptionStatus status)
        => new() { IsSuccess = true, TrackingCode = trackingCode, ResultStatus = status };
    public static EditPrescriptionResult Failure(string error, PrescriptionStatus status)
        => new() { IsSuccess = false, ErrorMessage = error, ResultStatus = status };
}

/// <summary>Cancels a previously accepted prescription.</summary>
public sealed record CancelPrescriptionCommand : IRequest<CancelPrescriptionResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record CancelPrescriptionResult
{
    public bool IsSuccess { get; init; }
    public PrescriptionStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static CancelPrescriptionResult Success(PrescriptionStatus status)
        => new() { IsSuccess = true, ResultStatus = status };
    public static CancelPrescriptionResult Failure(string error, PrescriptionStatus status)
        => new() { IsSuccess = false, ErrorMessage = error, ResultStatus = status };
}
