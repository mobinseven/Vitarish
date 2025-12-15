namespace Vitarish.Clients.Interface.Sea.Plugins.Clinic.Projects;
/// <summary>
/// Presents a patient intake workflow tailored for outpatient clinics. The form
/// collects demographic details, insurance information, and visit context so the
/// resulting project record contains the metadata clinicians rely on during the
/// first consultation.
/// </summary>
public sealed class ClinicPatientIntakeProjectCreationFormPlugin(
    IStringLocalizer localizer) : IProjectCreationFormPlugin
{
    private const string FacilityTypeMetadataKey = "FacilityType";
    private const string FacilityTypeClinicValue = "Clinic";

    /// <inheritdoc />
    public string Id => "clinic-patient-intake";

    /// <inheritdoc />
    public string DisplayName => localizer["DisplayName"];

    /// <inheritdoc />
    public string? Description => GetOptionalValue("Description");

    /// <inheritdoc />
    public bool IsAvailable(ProjectCreationFormContext context) => !context.TryGetMetadata<string>(FacilityTypeMetadataKey, out string? facilityType) || string.Equals(facilityType, FacilityTypeClinicValue, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public RenderFragment Render(ProjectCreationFormContext context) => builder =>
                                                                                 {
                                                                                     bool isTemplateCreation = context.TryGetMetadata<bool>(ProjectPluginsConstants.TemplateCreationMetadataKey, out bool templateFlag)
                                                                                     && templateFlag;

                                                                                     if (!isTemplateCreation && context.TryGetMetadata<string>(ProjectPluginsConstants.TemplateCreationMetadataKey, out string? templateFlagString)
                                                                                         && bool.TryParse(templateFlagString, out bool parsed))
                                                                                     {
                                                                                         isTemplateCreation = parsed;
                                                                                     }

                                                                                     if (isTemplateCreation)
                                                                                     {
                                                                                         builder.OpenComponent<ClinicTemplateProjectSetupForm>(0);
                                                                                         builder.AddAttribute(1, nameof(ClinicTemplateProjectSetupForm.Context), context);
                                                                                     }
                                                                                     else
                                                                                     {
                                                                                         builder.OpenComponent<ClinicPatientIntakeProjectCreationForm>(0);
                                                                                         builder.AddAttribute(1, nameof(ClinicPatientIntakeProjectCreationForm.Context), context);
                                                                                     }

                                                                                     builder.CloseComponent();
                                                                                 };

    private string? GetOptionalValue(string name)
    {
        LocalizedString localized = localizer[name];
        return localized.ResourceNotFound || string.IsNullOrWhiteSpace(localized.Value)
            ? null
            : localized.Value;
    }
}
