namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class ReminderDisplay : RemindersBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter, EditorRequired] public ReminderReadDto Reminder { get; set; } = default!;

    private async Task ShowDetails()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Reminder), Reminder }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ReminderDetailsDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is bool deleted && deleted)
        {
            await OnDeleted.InvokeAsync();
        }
    }
}
