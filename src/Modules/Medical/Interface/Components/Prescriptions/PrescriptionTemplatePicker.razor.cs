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
        try
        {
            templates = await Mediator.Send(new GetPrescriptionTemplatesQuery
            {
                TenantId = RequestContext.TenantId ?? string.Empty
            });
        }
        catch (Exception)
        {
            templates = [];
            selectedTemplate = null;
            selectedTemplateId = null;
            Snackbar.Add(Localizer["ErrorLoadingTemplates"], MudBlazor.Severity.Error);
        }
    }

    protected override Task ReloadRequired() => OnParametersSetAsync();

    private async Task OnTemplateSelectedAsync(Guid? templateId)
    {
        selectedTemplateId = templateId;
        if (templateId is null)
        {
            selectedTemplate = null;
            return;
        }

        try
        {
            selectedTemplate = await Mediator.Send(new GetPrescriptionTemplateQuery { TemplateId = templateId.Value, TenantId = RequestContext.TenantId ?? string.Empty });
        }
        catch (Exception ex)
        {
            selectedTemplate = null;
            Snackbar.Add(ex.Message, MudBlazor.Severity.Error);
        }
    }

    private async Task LoadAsync()
    {
        if (selectedTemplateId is null || PrescriptionId == Guid.Empty)
        {
            Snackbar.Add(Localizer["UnknownError"], MudBlazor.Severity.Error);
            return;
        }
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
        Snackbar.Add(FormatLoadError(result), MudBlazor.Severity.Error);
    }

    private static string FormatLoadError(PrescriptionTemplateCommandResult result)
    {
        if (result.ItemFailures.Count == 0)
        {
            return result.ErrorMessage ?? string.Empty;
        }

        string failures = string.Join(Environment.NewLine, result.ItemFailures.Select(f => $"{f.DisplayName ?? f.ReferenceCode ?? f.Category.ToString()}: {f.Message}"));
        return string.IsNullOrWhiteSpace(result.ErrorMessage) ? failures : $"{result.ErrorMessage}{Environment.NewLine}{failures}";
    }
}
