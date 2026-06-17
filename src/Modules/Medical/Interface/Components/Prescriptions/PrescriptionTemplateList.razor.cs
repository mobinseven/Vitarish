namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionTemplateList : PrescriptionBase
{
    [Parameter] public EventCallback<Guid> OnSelected { get; set; }
    [Parameter] public EventCallback OnCreate { get; set; }

    private IReadOnlyList<PrescriptionTemplateSummaryDto> templates = [];
    private bool includeArchived;

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override Task OnInitializedAsync() => ReloadRequired();

    protected override async Task ReloadRequired()
    {
        templates = await Mediator.Send(new GetPrescriptionTemplatesQuery
        {
            TenantId = RequestContext.TenantId ?? string.Empty,
            IncludeArchived = includeArchived,
            SearchTerm = SearchString
        });
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnIncludeArchivedChanged(bool value)
    {
        includeArchived = value;
        await ReloadRequired();
    }

    private async Task ArchiveAsync(Guid templateId)
    {
        PrescriptionTemplateCommandResult result = await Mediator.Send(new ArchivePrescriptionTemplateCommand
        {
            TemplateId = templateId,
            TenantId = RequestContext.TenantId ?? string.Empty
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["Saved"], MudBlazor.Severity.Success);
            await ReloadRequired();
            return;
        }

        Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], MudBlazor.Severity.Error);
    }

    private static string TemplateSummary(PrescriptionTemplateSummaryDto template) =>
        string.IsNullOrWhiteSpace(template.Description)
            ? $"{template.ItemCount} item(s)"
            : $"{template.Description} — {template.ItemCount} item(s)";
}
