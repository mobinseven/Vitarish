namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class ReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid SourceId { get; set; }

    private ReminderKind kind = ReminderKind.Sms;
    private List<ReminderRecipientReadDto> recipients = [];
    private DateTime remindAt = DateTime.Now.ToUniversalTime();

    private async Task SaveAsync()
    {
        CommunicationsQueryResult<IReadOnlyList<ReminderReadDto>> result = await CommunicationsClient.CreateRemindersAsync(new ReminderRangeCreateRequest(SourceId, recipients, string.Empty, string.Empty, remindAt), ComponentCancellationToken);
        MudDialog.Close(DialogResult.Ok(result.Value ?? []));
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
