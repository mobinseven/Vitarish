using Vitarish.Sea.Interface.Components.Events;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderAction : EventsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;

    private async Task AddReminderAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderCreatorDialog.Event), Event },
            { nameof(ReminderCreatorDialog.Procedure), Procedure },
            { nameof(ReminderCreatorDialog.Project), Project }
        };

        IDialogReference dialog = AdminMode
            ? await DialogService.ShowAsync<ReminderCreatorDialog>(Localizer["Reminder"], parameters)
            : await DialogService.ShowAsync<UserReminderCreatorDialog>(Localizer["Reminder"], parameters);

        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
    }
}
