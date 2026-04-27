namespace Vitarish.Reminders.Interface.Components.Reminders;

public partial class ReminderAction : RemindersBase
{
    [Parameter, EditorRequired] public Guid SourceId { get; set; }
    [Parameter] public EventCallback OnCreated { get; set; }

    private async Task AddReminderAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderCreatorDialog.SourceId), SourceId },
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
