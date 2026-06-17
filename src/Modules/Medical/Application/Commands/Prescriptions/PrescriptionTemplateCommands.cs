using MediatR;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.Commands.Prescriptions;

public sealed record CreatePrescriptionTemplateCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed record UpdatePrescriptionTemplateCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public required Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed record ArchivePrescriptionTemplateCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public required Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record AddPrescriptionTemplateItemCommand : IRequest<PrescriptionTemplateItemCommandResult>
{
    public required Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public required PrescriptionItemCategory Category { get; init; }
    public string? ReferenceCode { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? DosageJson { get; init; }
    public int Quantity { get; init; } = 1;
    public int? DurationDays { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed record RemovePrescriptionTemplateItemCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public required Guid TemplateId { get; init; }
    public required Guid ItemId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record LoadPrescriptionTemplateIntoDraftCommand : IRequest<LoadPrescriptionTemplateIntoDraftResult>
{
    public required Guid TemplateId { get; init; }
    public required Guid PrescriptionId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record PrescriptionTemplateCommandResult
{
    public bool IsSuccess { get; init; }
    public Guid? TemplateId { get; init; }
    public string? ErrorMessage { get; init; }
    public static PrescriptionTemplateCommandResult Success(Guid id) => new() { IsSuccess = true, TemplateId = id };
    public static PrescriptionTemplateCommandResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public sealed record PrescriptionTemplateItemCommandResult
{
    public bool IsSuccess { get; init; }
    public Guid? ItemId { get; init; }
    public string? ErrorMessage { get; init; }
    public static PrescriptionTemplateItemCommandResult Success(Guid id) => new() { IsSuccess = true, ItemId = id };
    public static PrescriptionTemplateItemCommandResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public sealed record LoadPrescriptionTemplateIntoDraftResult
{
    public bool IsSuccess { get; init; }
    public int ItemsCopied { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public static LoadPrescriptionTemplateIntoDraftResult Success(int count) => new() { IsSuccess = true, ItemsCopied = count };
    public static LoadPrescriptionTemplateIntoDraftResult Failure(params string[] errors) => new() { IsSuccess = false, Errors = errors };
}
