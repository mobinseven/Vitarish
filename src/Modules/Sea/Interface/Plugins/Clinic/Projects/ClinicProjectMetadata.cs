namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public sealed record ClinicProjectMetadata
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DateOfBirth { get; init; }

    public string? Gender { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? InsuranceProvider { get; init; }

    public string? PolicyNumber { get; init; }

    public string? PrimaryPhysician { get; init; }

    public string? VisitReason { get; init; }

    public string? PreferredPharmacy { get; init; }

    public string? Allergies { get; init; }

    public string? CurrentMedications { get; init; }

    public string? ClinicalNotes { get; init; }

    public string? EmergencyContactName { get; init; }

    public string? EmergencyContactPhone { get; init; }

    public bool? ConsentToShareRecords { get; init; }

    public Guid? FacilityId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? TemplateId { get; init; }
}
