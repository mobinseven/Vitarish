using System.ComponentModel;
using Vitarish.Clients.Interface.Sea.Plugins.Clinic.Projects;

namespace Vitarish.Clients.Interface.Sea.Plugins.Clinic.Tools;

[McpServerToolType]
public sealed class ClinicProjectsTools(
    IProjectsService ProjectsService,
    IStringLocalizer<ClinicPatientIntakeProjectCreationForm> Localizer)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = nameof(CreateClinicPatientProjectAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("Create a clinic intake project using patient metadata.")]
    public async Task<Guid> CreateClinicPatientProjectAsync(ClinicProjectDto input, CancellationToken cancellationToken = default)
    {
        ClinicPatientIntakeMetadata metadata = Normalize(input);
        string metadataJson = JsonSerializer.Serialize(metadata, SerializerOptions);
        string name = BuildDisplayName(metadata);
        string description = BuildDescription(metadata);
        return await ProjectsService.AddAsync(name, description, template: false, metadataJson, cancellationToken);
    }

    private ClinicPatientIntakeMetadata Normalize(ClinicProjectDto input) => new()
    {
        FirstName = Trim(input.FirstName),
        LastName = Trim(input.LastName),
        DateOfBirth = NormalizeDate(input.DateOfBirth),
        Gender = NormalizeGender(input.Gender),
        Email = Trim(input.Email),
        Phone = Trim(input.Phone),
        InsuranceProvider = Trim(input.InsuranceProvider),
        PolicyNumber = Trim(input.PolicyNumber),
        PrimaryPhysician = Trim(input.PrimaryPhysician),
        VisitReason = Trim(input.VisitReason),
        PreferredPharmacy = Trim(input.PreferredPharmacy),
        Allergies = Trim(input.Allergies),
        CurrentMedications = Trim(input.CurrentMedications),
        ClinicalNotes = Trim(input.ClinicalNotes),
        EmergencyContactName = Trim(input.EmergencyContactName),
        EmergencyContactPhone = Trim(input.EmergencyContactPhone),
        ConsentToShareRecords = input.ConsentToShareRecords ?? true,
        FacilityId = input.FacilityId,
        DepartmentName = Trim(input.DepartmentName),
        TemplateId = input.TemplateId
    };

    private static string? NormalizeDate(string? value)
    {
        string? trimmed = Trim(value);
        return trimmed is null
            ? null
            : DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : trimmed;
    }

    private static string NormalizeGender(string? value)
    {
        string? trimmed = Trim(value);
        return trimmed is null
            ? ClinicPatientIntakeProjectCreationForm.GenderUnspecified
            : string.Equals(trimmed, ClinicPatientIntakeProjectCreationForm.GenderFemale, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "F", StringComparison.OrdinalIgnoreCase)
            ? ClinicPatientIntakeProjectCreationForm.GenderFemale
            : string.Equals(trimmed, ClinicPatientIntakeProjectCreationForm.GenderMale, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "M", StringComparison.OrdinalIgnoreCase)
            ? ClinicPatientIntakeProjectCreationForm.GenderMale
            : ClinicPatientIntakeProjectCreationForm.GenderUnspecified;
    }

    private string BuildDescription(ClinicPatientIntakeMetadata metadata) => string.IsNullOrWhiteSpace(metadata.ClinicalNotes)
                ? Localizer["Description_OutpatientIntakeRecord"]
                : metadata.ClinicalNotes!;

    private string BuildDisplayName(ClinicPatientIntakeMetadata metadata)
    {
        string displayName = string.Join(" ", new[] { metadata.FirstName, metadata.LastName }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim()));
        return string.IsNullOrWhiteSpace(displayName)
            ? Localizer["Default_ClinicPatientName"]
            : displayName;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
