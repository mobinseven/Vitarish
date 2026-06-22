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
    public Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed record ArchivePrescriptionTemplateCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record AddPrescriptionTemplateItemCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public Guid TemplateId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public PrescriptionItemCategory Category { get; init; }
    public string? ReferenceCode { get; init; }
    public string? DisplayName { get; init; }
    public string? DosageJson { get; init; }
    public int Quantity { get; init; } = 1;
    public int? DurationDays { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed record RemovePrescriptionTemplateItemCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public Guid TemplateId { get; init; }
    public Guid ItemId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record LoadPrescriptionTemplateIntoDraftCommand : IRequest<PrescriptionTemplateCommandResult>
{
    public Guid TemplateId { get; init; }
    public Guid PrescriptionId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}

public sealed record PrescriptionTemplateCommandResult
{
    public bool IsSuccess { get; init; }
    public Guid? Id { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<PrescriptionTemplateItemFailure> ItemFailures { get; init; } = [];

    public static PrescriptionTemplateCommandResult Success(Guid? id = null) => new() { IsSuccess = true, Id = id };
    public static PrescriptionTemplateCommandResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
    public static PrescriptionTemplateCommandResult Failure(string error, IReadOnlyList<PrescriptionTemplateItemFailure> itemFailures)
        => new() { IsSuccess = false, ErrorMessage = error, ItemFailures = itemFailures };
}

public sealed record PrescriptionTemplateItemFailure
{
    public Guid TemplateItemId { get; init; }
    public PrescriptionItemCategory Category { get; init; }
    public string? ReferenceCode { get; init; }
    public string? DisplayName { get; init; }
    public string Message { get; init; } = string.Empty;
}
