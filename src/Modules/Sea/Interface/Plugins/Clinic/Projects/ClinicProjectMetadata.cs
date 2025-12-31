namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

internal static class ClinicProjectMetadataParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool HasClinicMetadata(string? metadataJson) => TryParsePatient(metadataJson) is not null || TryParseTemplate(metadataJson) is not null;

    public static ClinicPatientIntakeMetadata? TryParsePatient(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            ClinicPatientIntakeMetadata? metadata = JsonSerializer.Deserialize<ClinicPatientIntakeMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.FirstName)
                && string.IsNullOrWhiteSpace(metadata.LastName)
                && string.IsNullOrWhiteSpace(metadata.VisitReason)
                && string.IsNullOrWhiteSpace(metadata.InsuranceProvider)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static ClinicTemplateMetadata? TryParseTemplate(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            ClinicTemplateMetadata? metadata = JsonSerializer.Deserialize<ClinicTemplateMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.DefaultVisitReason)
                && string.IsNullOrWhiteSpace(metadata.IntakeChecklist)
                && string.IsNullOrWhiteSpace(metadata.BaselineConsentText)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record ClinicPatientIntakeMetadata
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

public sealed record ClinicTemplateMetadata
{
    public string? DefaultDepartmentName { get; init; }

    public string? DefaultPrimaryPhysician { get; init; }

    public string? DefaultVisitReason { get; init; }

    public string? IntakeChecklist { get; init; }

    public string? BaselineConsentText { get; init; }

    public bool IncludeInsuranceSection { get; init; }

    public bool IncludeClinicalNotesSection { get; init; }

    public Guid? FacilityId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? TemplateId { get; init; }
}
