namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationReminderList : ReservationsBase
{
    [Parameter, EditorRequired] public IReadOnlyList<ReminderReadDto> Reminders { get; set; } = [];
    [Parameter] public bool CanManage { get; set; }
    [Parameter] public EventCallback<ReminderReadDto> EditReminder { get; set; }

    private string ReminderLabel(ReminderReadDto reminder)
        => $"{Localizer[reminder.Kind.ToString()]} · {reminder.RemindAt.ToLocalTime().DateTime:g}";

    private string ReminderHelperText(ReminderReadDto reminder)
    {
        string recipient = string.IsNullOrWhiteSpace(reminder.RecipientUserId)
            ? reminder.Recipient
            : GetUserFullNameById(reminder.RecipientUserId) ?? reminder.Recipient;
        string status = reminder.IsSent
            ? $"{Localizer["SentAt"]}: {reminder.SentAt?.ToLocalTime().DateTime:g}"
            : Localizer["NotSent"].Value;

        return $"{Localizer["Recipient"]}: {recipient} · {status}";
    }
}
