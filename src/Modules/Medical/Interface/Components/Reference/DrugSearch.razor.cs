namespace Vitarish.Medical.Interface.Components.Reference;

public partial class DrugSearch : MedicalBase
{
    private List<Drug> results = [];
    private string searchText = string.Empty;
    private bool searched;

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        results = [.. await MedicalClient.SearchDrugsAsync(searchText)];
        searched = true;
        await InvokeAsync(StateHasChanged);
    }
}
