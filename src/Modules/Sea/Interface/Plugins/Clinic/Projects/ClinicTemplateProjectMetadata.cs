namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public sealed record ClinicTemplateProjectMetadata
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
