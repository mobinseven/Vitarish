using Vitarish.Clinic.Contracts.Enums;
using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Clinic.Contracts.Responses;

public sealed record PatientListRequest(string? Search = null, int? Skip = null, int? Take = null, bool? Template = false, PatientSortBy? SortBy = PatientSortBy.Name, bool Descending = false);
public sealed record PatientCountRequest(string? Search = null, bool? Template = false);
public sealed record PatientSearchRequest(string? Search = null, int? Skip = null, int? Take = null, bool? Template = false);
public sealed record PatientIdentificationLookupRequest(string IdentificationCode);
public sealed record PatientFindOrCreateRequest(
    string? IdentificationCode,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    DateTime? DateOfBirth = null,
    DateTime? DateOfSurgery = null,
    Gender? Gender = null,
    string? PlaceOfBirth = null,
    string? Address = null,
    Education? Education = null,
    string? Occupation = null,
    string? FatherName = null,
    int? Height = null,
    int? Weight = null,
    string? Referrer = null,
    string? InsuranceProvider = null,
    string? VisitReason = null,
    string? Allergies = null,
    string? CurrentMedications = null,
    string? ClinicalNotes = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null);
public sealed record PatientDetailsRequest(Guid PatientId);

public sealed class PatientViewDto
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? AvatarFileId { get; init; }
    public string? IdentificationCode { get; init; }
    public bool Template { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public DateTime? DeletedAt { get; init; }
    public string? DeletedBy { get; init; }
    public bool IsDeleted { get; init; }
    public string[] UserIds { get; init; } = [];
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public DateTime? DateOfSurgery { get; init; }
    public Gender Gender { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? Address { get; init; }
    public Education Education { get; init; }
    public string? Occupation { get; init; }
    public string? FatherName { get; init; }
    public int Height { get; init; }
    public int Weight { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Referrer { get; init; }
    public string? InsuranceProvider { get; init; }
    public string? VisitReason { get; init; }
    public string? Allergies { get; init; }
    public string? CurrentMedications { get; init; }
    public string? ClinicalNotes { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public StatsReadDto? Stats { get; init; }
}
