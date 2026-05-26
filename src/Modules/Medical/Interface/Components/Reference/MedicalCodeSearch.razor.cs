namespace Vitarish.Medical.Interface.Components.Reference;

public partial class MedicalCodeSearch : MedicalBase
{
    private List<CodeSystem> codeSystems = [];
    private List<MedicalCode> results = [];
    private string systemKey = string.Empty;
    private string searchText = string.Empty;
    private bool searched;

    protected override async Task ReloadRequired()
    {
        codeSystems = [.. await QueryService.ListCodeSystemsAsync(activeOnly: true)];
        if (codeSystems.Count > 0 && string.IsNullOrEmpty(systemKey))
        {
            systemKey = codeSystems[0].SystemKey;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(systemKey) || string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        results = [.. await QueryService.SearchMedicalCodesAsync(systemKey, searchText)];
        searched = true;
        await InvokeAsync(StateHasChanged);
    }
}
