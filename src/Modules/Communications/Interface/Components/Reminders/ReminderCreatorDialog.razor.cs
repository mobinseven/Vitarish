namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class ReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid SourceId { get; set; }

    private ReminderKind kind = ReminderKind.Sms;
    private List<ReminderRecipientReadDto> recipients = [];
    private DateTime? remindDate = DateTime.UtcNow.Date;
    private TimeSpan? remindTime = DateTime.UtcNow.TimeOfDay;
    private DateTime RemindAt => DateTime.SpecifyKind(remindDate!.Value.Date + remindTime!.Value, DateTimeKind.Local).ToUniversalTime();

    private async Task SaveAsync()
    {
        IReadOnlyList<ReminderReadDto> added = await RemindersService.AddRangeAsync(SourceId, recipients, string.Empty, string.Empty, RemindAt, CancellationToken.None);
        MudDialog.Close(DialogResult.Ok(added));
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task AddRecipientAsync()
    {
        DialogParameters parameters = new() { [nameof(AddressBookDialog.Kind)] = kind, [nameof(AddressBookDialog.Recipients)] = recipients };
        IDialogReference dialog = await DialogService.ShowAsync<AddressBookDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (!result.Canceled && result.Data is IEnumerable<ReminderRecipientReadDto> rec)
        {
            recipients = [.. rec];
        }
    }
}
