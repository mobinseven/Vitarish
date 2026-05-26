namespace Vitarish.Medical.Interface.Components.Imports;

public partial class ImportBatchDetail : MedicalBase
{
    [Parameter, EditorRequired] public Guid ImportBatchId { get; set; }

    private ImportBatch? batch;
    private bool loading;

    protected override async Task ReloadRequired()
    {
        loading = true;
        await InvokeAsync(StateHasChanged);

        batch = await QueryService.GetImportBatchAsync(ImportBatchId);
        loading = false;
        await InvokeAsync(StateHasChanged);
    }
}
