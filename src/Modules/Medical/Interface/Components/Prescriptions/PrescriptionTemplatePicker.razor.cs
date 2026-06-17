namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionTemplatePicker : PrescriptionBase
{
    [Parameter, EditorRequired] public Guid PrescriptionId { get; set; }
    [Parameter] public EventCallback OnLoaded { get; set; }

    private IReadOnlyList<PrescriptionTemplateSummaryDto> templates = [];
    private Guid? selectedTemplateId;
    private PrescriptionTemplateDto? selectedTemplate;

    protected override async Task OnParametersSetAsync()
    {
        templates = await Mediator.Send(new GetPrescriptionTemplatesQuery
        {
            TenantId = RequestContext.TenantId ?? string.Empty
        });
    }

    protected override Task ReloadRequired() => OnParametersSetAsync();

    private async Task OnTemplateSelectedAsync(Guid? templateId)
    {
        selectedTemplateId = templateId;
        selectedTemplate = templateId is null
            ? null
            : await Mediator.Send(new GetPrescriptionTemplateQuery { TemplateId = templateId.Value, TenantId = RequestContext.TenantId ?? string.Empty });
    }

    private async Task LoadAsync()
    {
        if (selectedTemplateId is null) return;
        PrescriptionTemplateCommandResult result = await Mediator.Send(new LoadPrescriptionTemplateIntoDraftCommand
        {
            TemplateId = selectedTemplateId.Value,
            PrescriptionId = PrescriptionId,
            TenantId = RequestContext.TenantId ?? string.Empty
        });
        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["Saved"], MudBlazor.Severity.Success);
            await OnLoaded.InvokeAsync();
            return;
        }
        Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], MudBlazor.Severity.Error);
    }
}
