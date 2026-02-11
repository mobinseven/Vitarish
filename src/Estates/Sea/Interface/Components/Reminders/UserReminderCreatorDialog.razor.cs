namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class UserReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;

    private ReminderKind kind = ReminderKind.Sms;
    private DateTime? remindDate = DateTime.Now.Date;
    private TimeSpan? remindTime = DateTime.Now.TimeOfDay;
    private DateTimeOffset RemindAt => new(remindDate!.Value.Date + remindTime!.Value, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        string? phone = RequestContext.User?.GetUserPhoneNumber();
        string? email = RequestContext.User?.GetUserEmail();
        kind = !string.IsNullOrWhiteSpace(phone) ? ReminderKind.Sms : ReminderKind.Email;
    }

    private async Task SaveAsync()
    {
        List<ReminderRecipientReadDto> recipients = [new ReminderRecipientReadDto(kind, RequestContext.UserId, GetAddress(kind))];
        IReadOnlyList<ReminderReadDto> added = await RemindersService.AddRangeAsync(Event.Id, recipients, string.Empty, string.Empty, RemindAt, CancellationToken.None);
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
