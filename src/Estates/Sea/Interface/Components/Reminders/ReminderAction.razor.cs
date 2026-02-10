namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderAction : EventsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter] public EventCallback OnCreated { get; set; }
    private async Task AddReminderAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderCreatorDialog.Event), Event },
        };

        IDialogReference dialog = AdminMode
            ? await DialogService.ShowAsync<ReminderCreatorDialog>(Localizer["Reminder"], parameters)
            : await DialogService.ShowAsync<UserReminderCreatorDialog>(Localizer["Reminder"], parameters);

        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await OnCreated.InvokeAsync();
    }
}
