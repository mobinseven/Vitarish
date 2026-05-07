namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationRemindersPanel : ReservationsBase
{
    [Parameter, EditorRequired] public ReservationReadDto Reservation { get; set; } = default!;

    private IReadOnlyList<ReminderReadDto> reminders = [];
    private bool isLoading;
    private string? errorMessage;

    private bool CanManage => !IsTerminal(Reservation.Status);
    private bool CanAddReminder => HasAnyContact && Reservation.Occurrence is not null;
    private bool HasAnyContact => !string.IsNullOrWhiteSpace(RequestContext.User?.GetUserEmail())
        || !string.IsNullOrWhiteSpace(RequestContext.User?.GetUserPhoneNumber());

    private string PanelHelperText => CanManage
        ? Localizer["ReservationReminderHelp"].Value
        : Localizer["TerminalReservationReminderHelp"].Value;

    protected override async Task OnParametersSetAsync()
    {
        await LoadRemindersAsync();
    }

    private async Task LoadRemindersAsync()
    {
        if (Reservation.Id == Guid.Empty)
        {
            reminders = [];
            return;
        }

        isLoading = true;
        errorMessage = null;
        try
        {
            EntityResult<IReadOnlyList<ReminderReadDto>> result = await AppointmentReminderService.GetUserReservationRemindersAsync(
                Reservation.Id,
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                reminders = [];
                errorMessage = FormatErrors(result.Errors);
                return;
            }

            reminders = result.Value;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task AddReminderAsync() => await ShowReminderDialogAsync(null);

    private async Task EditReminderAsync(ReminderReadDto reminder)
    {
        if (!CanManage)
        {
            return;
        }

        await ShowReminderDialogAsync(reminder);
    }

    private async Task ShowReminderDialogAsync(ReminderReadDto? reminder)
    {
        DialogParameters parameters = new()
        {
            { nameof(ReservationReminderDialog.Reservation), Reservation },
            { nameof(ReservationReminderDialog.Reminder), reminder }
        };

        string title = reminder is null
            ? Localizer["AddItem", Localizer["Reminder"]].Value
            : Localizer["EditItem", Localizer["Reminder"]].Value;
        IDialogReference dialog = await DialogService.ShowAsync<ReservationReminderDialog>(title, parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await LoadRemindersAsync();
    }

    private static string FormatErrors(IEnumerable<EntityError> errors)
        => string.Join(Environment.NewLine, errors.Select(static error => error.Message));
}
