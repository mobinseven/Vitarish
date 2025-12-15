namespace Vitarish.Clients.Interface.Sea.Plugins.Gym.Projects;

public partial class GymTemplateProjectSetupForm : ProjectsBase
{
    private const string FacilityIdMetadataKey = "FacilityId";
    private const string FacilityNameMetadataKey = "FacilityName";

    private readonly GymTemplateFormModel formModel = new();

    private MudForm? form;
    private bool isSubmitting;
    private Guid? facilityId;
    private string? facilityLabel;
    private string? errorMessage;

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter]
    public required ProjectCreationFormContext Context { get; set; }

    private string submitLabel => isSubmitting
        ? Localizer["Creating..."]
        : Localizer["Create template"];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Context.TryGetMetadata<Guid>(FacilityIdMetadataKey, out Guid facility))
        {
            facilityId = facility;
        }

        facilityLabel = Context.TryGetMetadata<string>(FacilityNameMetadataKey, out string? facilityName)
            ? (string)Localizer["Facility: {0}", facilityName!]
            : null;
    }

    private async Task SubmitAsync()
    {
        if (form is null)
        {
            return;
        }

        errorMessage = null;
        await form.Validate();
        if (isSubmitting || form.IsValid is not true)
        {
            return;
        }

        isSubmitting = true;

        try
        {
            CancellationToken cancellationToken = CancellationToken.None;
            string templateName = formModel.TemplateName;
            string description = string.IsNullOrWhiteSpace(formModel.TemplateDescription)
                ? Localizer["Gym trainee project template"]
                : formModel.TemplateDescription!;

            string metadata = JsonSerializer.Serialize(new GymTemplateMetadata
            {
                DefaultMembershipType = formModel.DefaultMembershipType,
                DefaultPreferredProgram = formModel.DefaultPreferredProgram,
                DefaultGoals = formModel.DefaultGoals,
                OnboardingChecklist = formModel.OnboardingChecklist,
                SafetyGuidelines = formModel.SafetyGuidelines,
                IncludeEmergencyContact = formModel.IncludeEmergencyContact,
                IncludeMedicalNotes = formModel.IncludeMedicalNotes,
                FacilityId = facilityId,
                TemplateId = Context.TemplateId
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            _ = await ProjectsService.AddAsync(templateName, description, template: true, metadata, cancellationToken);

            Dialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();

    private sealed class GymTemplateFormModel
    {
        [Required, MaxLength(128)]
        public string TemplateName { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? TemplateDescription { get; set; }
            = string.Empty;

        [MaxLength(128)]
        public string? DefaultMembershipType { get; set; }
            = string.Empty;

        [MaxLength(128)]
        public string? DefaultPreferredProgram { get; set; }
            = string.Empty;

        [Required, MaxLength(256)]
        public string DefaultGoals { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? OnboardingChecklist { get; set; }
            = string.Empty;

        [MaxLength(512)]
        public string? SafetyGuidelines { get; set; }
            = string.Empty;

        public bool IncludeEmergencyContact { get; set; } = true;

        public bool IncludeMedicalNotes { get; set; } = true;
    }
}
