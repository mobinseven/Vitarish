namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;

    private ReminderKind kind = ReminderKind.Sms;
    private List<ReminderRecipientReadDto> recipients = [];
    private DateTime? remindDate = DateTime.Now.Date;
    private TimeSpan? remindTime = DateTime.Now.ToLocalTime().TimeOfDay;
    private DateTimeOffset RemindAt => new(remindDate!.Value.Date + remindTime!.Value, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));

    private async Task SaveAsync()
    {
        IReadOnlyList<ReminderReadDto> added = await RemindersService.AddRangeAsync(Event.Id, recipients, string.Empty, string.Empty, RemindAt, CancellationToken.None);
        MudDialog.Close(DialogResult.Ok(added));
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task AddRecipientAsync()
    {
        DialogParameters parameters = new() { [nameof(AddressBookDialog.Kind)] = kind, [nameof(AddressBookDialog.Recipients)] = recipients };
        IDialogReference dialog = await DialogService.ShowAsync<AddressBookDialog>(Localizer["Receivers"], parameters);
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
