namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class UserReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid SourceId { get; set; }

    private ReminderKind kind = ReminderKind.Sms;
    private DateTime remindAt = DateTime.Now.ToUniversalTime();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        string? phone = RequestContext.User?.GetUserPhoneNumber();

        _ = RequestContext.User?.GetUserEmail();
        kind = !string.IsNullOrWhiteSpace(phone) ? ReminderKind.Sms : ReminderKind.Email;
    }

    private async Task SaveAsync()
    {
        List<ReminderRecipientReadDto> recipients = [new ReminderRecipientReadDto { Kind = kind, RecipientUserId = RequestContext.UserId, Recipient = GetAddress(kind) }];
        IReadOnlyList<ReminderReadDto> added = await RemindersService.AddRangeAsync(SourceId, recipients, string.Empty, string.Empty, remindAt, CancellationToken.None);
        MudDialog.Close(DialogResult.Ok(added));
    }

    private void Cancel() => MudDialog.Cancel();
    private string? GetAddress(ReminderKind kind)
        => kind switch
        {
            ReminderKind.Email => RequestContext.User?.GetUserEmail(),
            ReminderKind.Sms => RequestContext.User?.GetUserPhoneNumber(),
        };
}
