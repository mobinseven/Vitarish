namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderDisplay : SeaCommonBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public ReminderDto Reminder { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;

    private async Task ShowDetails()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Event), Event },
            { nameof(ReminderDetailsDialog.Project), Project },
            { nameof(ReminderDetailsDialog.Procedure), Procedure },
            { nameof(ReminderDetailsDialog.Reminder), Reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(Localizer["Reminder"], parameters);
    }
    private string? UserIdToName(string? id) => id is null ? null : AccessService.Users.FirstOrDefault(x => x.Id == id)?.FullName;

}

