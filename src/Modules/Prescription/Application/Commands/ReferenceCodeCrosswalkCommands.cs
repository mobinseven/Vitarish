using MediatR;

namespace Vitarish.Prescription.Application.Commands;

/// <summary>Creates or updates a code crosswalk mapping.</summary>
public sealed record UpsertReferenceCodeCrosswalkCommand : IRequest<UpsertCrosswalkResult>
{
    public required string LocalCode { get; init; }
    public required string LocalCodeSystem { get; init; }
    public required string ProviderKey { get; init; }
    public required string ProviderCode { get; init; }
    public required string ProviderCodeSystem { get; init; }

    /// <summary>0.0 to 1.0; default 1.0 for manual mappings.</summary>
    public decimal MappingConfidence { get; init; } = 1.0m;
}

public sealed record UpsertCrosswalkResult
{
    public bool IsSuccess { get; init; }
    public Guid? CrosswalkId { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpsertCrosswalkResult Success(Guid id) => new() { IsSuccess = true, CrosswalkId = id };
    public static UpsertCrosswalkResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Deactivates a crosswalk mapping.</summary>
public sealed record DeactivateReferenceCodeCrosswalkCommand : IRequest<bool>
{
    public Guid CrosswalkId { get; init; }
}

/// <summary>Bulk import crosswalks from CSV.</summary>
public sealed record BulkImportCrosswalkCommand : IRequest<BulkImportResult>
{
    /// <summary>CSV content: LocalCode,LocalCodeSystem,ProviderKey,ProviderCode,ProviderCodeSystem,Confidence</summary>
    public required string CsvContent { get; init; }

    /// <summary>Whether to skip duplicates or error.</summary>
    public bool SkipDuplicates { get; init; } = true;
}

public sealed record BulkImportResult
{
    public bool IsSuccess { get; init; }
    public int SuccessCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
