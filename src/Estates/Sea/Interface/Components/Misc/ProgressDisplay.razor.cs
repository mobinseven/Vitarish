namespace Vitarish.Sea.Interface.Components.Misc;

public partial class ProgressDisplay
{
    [Parameter, EditorRequired] public StatsDto Stats { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; }

    private string ProgressIcon(ProgressState? state) => state switch
    {
        ProgressState.NotStarted => "fa-regular fa-circle",
        ProgressState.Quarter => "fa-regular fa-circle-quarter",
        ProgressState.Half => "fa-regular fa-circle-half",
        ProgressState.ThreeQuarters => "fa-regular fa-circle-three-quarters",
        ProgressState.InProgress => "fa-duotone fa-regular fa-circle-three-quarters",
        ProgressState.Completed => "fa-regular fa-circle-check",
        _ => "fa-regular fa-circle-dotted"
    };

    private async Task DisplayInfo()
    {
        DialogParameters parameters = new()
        {
            { nameof(StatsDialog.Stats), Stats },
            { nameof(StatsDialog.Icon), ProgressIcon(Stats.State) },
        };

        _ = await DialogService.ShowAsync<StatsDialog>(Title, parameters);
    }
}
