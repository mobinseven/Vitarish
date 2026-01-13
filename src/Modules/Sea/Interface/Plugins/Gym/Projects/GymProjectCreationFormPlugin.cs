namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;
/// <summary>
/// Sample plugin that surfaces a gym trainee onboarding form when users create
/// a new project. The plugin demonstrates how metadata can be captured and
/// persisted alongside the project record.
/// </summary>
public sealed class GymProjectCreationFormPlugin(
    IStringLocalizer localizer) : IProjectCreationFormPlugin
{
    private const string FacilityTypeMetadataKey = "FacilityType";
    private const string FacilityTypeGymValue = "Gym";

    /// <inheritdoc />
    public string Id => "gym-trainee";

    /// <inheritdoc />
    public string DisplayName => localizer["DisplayName"];

    /// <inheritdoc />
    public string? Description => GetOptionalValue("Description");

    /// <inheritdoc />
    public bool IsAvailable(ProjectCreationFormContext context) => !context.TryGetMetadata<string>(FacilityTypeMetadataKey, out string? facilityType) || string.Equals(facilityType, FacilityTypeGymValue, StringComparison.OrdinalIgnoreCase);

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
                                                                                         builder.OpenComponent<GymTemplateProjectSetupForm>(0);
                                                                                         builder.AddAttribute(1, nameof(GymTemplateProjectSetupForm.Context), context);
                                                                                     }
                                                                                     else
                                                                                     {
                                                                                         builder.OpenComponent<GymProjectCreationForm>(0);
                                                                                         builder.AddAttribute(1, nameof(GymProjectCreationForm.Context), context);
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
