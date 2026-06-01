using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Queries;

/// <summary>Gets all crosswalks for a local code, resolved across providers.</summary>
public sealed record GetReferenceCrosswalksByLocalCodeQuery : IRequest<IReadOnlyList<ProviderCodeMappingDto>>
{
    public required string LocalCode { get; init; }
    public required string LocalCodeSystem { get; init; }
    public string? ProviderKey { get; init; }
}

/// <summary>Gets code selector options for UI dropdown, filtered by active codes for selected provider.</summary>
public sealed record GetCodeSelectorOptionsQuery : IRequest<IReadOnlyList<CodeSelectorOptionDto>>
{
    public required string ProviderKey { get; init; }
    public required string CodeSystem { get; init; }
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public int Take { get; init; } = 50;
}

/// <summary>Gets crosswalk entries for admin review/edit.</summary>
public sealed record GetReferenceCodeCrosswalkGridQuery : IRequest<ReferenceCodeCrosswalkGridResult>
{
    public string? LocalCode { get; init; }
    public string? ProviderKey { get; init; }
    public bool? ShowInactiveOnly { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}

public sealed record ReferenceCodeCrosswalkGridResult
{
    public IReadOnlyList<CrosswalkGridRowDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

// DTOs

public sealed record ProviderCodeMappingDto
{
    public Guid CrosswalkId { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
}

public sealed record CodeSelectorOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public bool IsAvailable { get; init; } = true;
}

public sealed record CrosswalkGridRowDto
{
    public Guid Id { get; init; }
    public string LocalCode { get; init; } = string.Empty;
    public string LocalCodeSystem { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public string ProviderCode { get; init; } = string.Empty;
    public string ProviderCodeSystem { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
}
