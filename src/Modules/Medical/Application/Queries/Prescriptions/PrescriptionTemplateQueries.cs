using MediatR;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.Queries.Prescriptions;

public sealed record GetPrescriptionTemplatesQuery : IRequest<IReadOnlyList<PrescriptionTemplateSummaryDto>>
{
    public string TenantId { get; init; } = string.Empty;
    public bool IncludeArchived { get; init; }
    public string? SearchTerm { get; init; }
}

public sealed record GetPrescriptionTemplateQuery : IRequest<PrescriptionTemplateDto?>
{
    public Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public record PrescriptionTemplateSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PrescriptionTemplateStatus Status { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record PrescriptionTemplateDto : PrescriptionTemplateSummaryDto
{
    public IReadOnlyList<PrescriptionTemplateItemDto> Items { get; init; } = [];
}

public sealed record PrescriptionTemplateItemDto
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
