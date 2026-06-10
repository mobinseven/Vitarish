namespace Vitarish.Sea.Application.Queries.Patients;

public sealed record GetPatientViewsQuery(string? Search = null, int? Skip = null, int? Take = null) : IRequest<IReadOnlyList<PatientListItemDto>>;

public sealed record PatientListItemDto(
    Guid Id,
    string TenantId,
    string Name,
    string Description,
    string? IdentificationCode,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    string? FirstName,
    string? LastName,
    string? DateOfBirth,
    string? Phone,
    string? Email);
