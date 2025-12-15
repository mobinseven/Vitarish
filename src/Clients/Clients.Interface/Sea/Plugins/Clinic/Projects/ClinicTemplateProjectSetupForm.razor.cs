namespace Vitarish.Clients.Interface.Sea.Plugins.Clinic.Projects;

public partial class ClinicTemplateProjectSetupForm : ProjectsBase
{
    private const string FacilityIdMetadataKey = "FacilityId";
    private const string FacilityNameMetadataKey = "FacilityName";
    private const string DepartmentNameMetadataKey = "DepartmentName";

    private readonly ClinicTemplateFormModel formModel = new();

    private MudForm? form;
    private bool isSubmitting;
    private Guid? facilityId;
    private string? facilityLabel;
    private string? departmentName;
    private string? departmentLabel;
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

        if (Context.TryGetMetadata<string>(DepartmentNameMetadataKey, out string? department))
        {
            departmentName = department;
            departmentLabel = Localizer["Department: {0}", department!];
        }
        else
        {
            departmentName = null;
            departmentLabel = null;
        }
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
                ? Localizer["Clinic intake project template"]
                : formModel.TemplateDescription!;

            string metadata = JsonSerializer.Serialize(new ClinicTemplateMetadata(
                formModel.DefaultDepartmentName,
                formModel.DefaultPrimaryPhysician,
                formModel.DefaultVisitReason,
                formModel.IntakeChecklist,
                formModel.BaselineConsentText,
                formModel.IncludeInsuranceSection,
                formModel.IncludeClinicalNotesSection,
                facilityId,
                departmentName,
                Context.TemplateId), new JsonSerializerOptions(JsonSerializerDefaults.Web));

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

    private sealed record ClinicTemplateMetadata(
        string? DefaultDepartmentName,
        string? DefaultPrimaryPhysician,
        string DefaultVisitReason,
        string? IntakeChecklist,
        string? BaselineConsentText,
        bool IncludeInsuranceSection,
        bool IncludeClinicalNotesSection,
        Guid? FacilityId,
        string? DepartmentName,
        Guid? TemplateId);

    private sealed class ClinicTemplateFormModel
    {
        [Required, MaxLength(128)]
        public string TemplateName { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? TemplateDescription { get; set; }
            = string.Empty;

        [MaxLength(128)]
        public string? DefaultDepartmentName { get; set; }
            = string.Empty;

        [MaxLength(128)]
        public string? DefaultPrimaryPhysician { get; set; }
            = string.Empty;

        [Required, MaxLength(256)]
        public string DefaultVisitReason { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? IntakeChecklist { get; set; }
            = string.Empty;

        [MaxLength(512)]
        public string? BaselineConsentText { get; set; }
            = string.Empty;

        public bool IncludeInsuranceSection { get; set; } = true;

        public bool IncludeClinicalNotesSection { get; set; } = true;
    }
}
