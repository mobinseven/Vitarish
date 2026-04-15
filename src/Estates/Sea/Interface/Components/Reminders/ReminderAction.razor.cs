namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderAction : EventsBase
{
    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureReadDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;
    [Parameter] public EventCallback OnCreated { get; set; }
    private async Task AddReminderAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderCreatorDialog.Event), Event },
        };

        IDialogReference dialog = AdminMode
            ? await DialogService.ShowAsync<ReminderCreatorDialog>(null, parameters)
            : await DialogService.ShowAsync<UserReminderCreatorDialog>(null, parameters);

        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await OnCreated.InvokeAsync();
    }
}
