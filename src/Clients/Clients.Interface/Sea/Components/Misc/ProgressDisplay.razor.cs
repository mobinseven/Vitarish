namespace Vitarish.Clients.Interface.Sea.Components.Misc;

public partial class ProgressDisplay
{
    [Parameter, EditorRequired] public StatsDto Stats { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; }

    private string ProgressIcon(ProgressState? state) => state switch
    {
        ProgressState.NotStarted => "fa-thin fa-circle",
        ProgressState.Quarter => "fa-solid fa-circle-quarter",
        ProgressState.Half => "fa-solid fa-circle-half",
        ProgressState.ThreeQuarters => "fa-solid fa-circle-three-quarters",
        ProgressState.InProgress => "fa-duotone fa-solid fa-circle-three-quarters",
        ProgressState.Completed => "fa-solid fa-circle-check",
        _ => "fa-solid fa-circle-dotted"
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
