namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderDetailsDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReminderReadDto Reminder { get; set; } = default!;

    private bool CanDelete => (AdminMode || RequestContext.UserId == Reminder.CreatedBy) && !Reminder.IsSent;

    private string SentAtText() => Reminder.SentAt is null
        ? Localizer["NotSent"].Value
        : $"{Reminder.SentAt.Value.ToLocalTime().DateTime:d} {Reminder.SentAt.Value.ToLocalTime().DateTime:t}";

    private async Task DeleteAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion", Localizer["Reminder"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["DeleteItem", Localizer["Reminder"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Reminder"]], parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        _ = await RemindersService.RemoveAsync(Reminder.Id, CancellationToken.None);
        MudDialog.Close(true);
    }

    private void Close() => MudDialog.Cancel();
}
