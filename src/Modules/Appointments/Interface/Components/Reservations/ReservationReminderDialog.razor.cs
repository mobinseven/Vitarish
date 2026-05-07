namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationReminderDialog : ReservationsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReservationReadDto Reservation { get; set; } = default!;
    [Parameter] public ReminderReadDto? Reminder { get; set; }

    private ReminderKind kind = ReminderKind.Sms;
    private DateTime? remindDate;
    private TimeSpan? remindTime;
    private string? errorMessage;
    private bool isSaving;

    private bool HasEmail => !string.IsNullOrWhiteSpace(CurrentEmail);
    private bool HasSms => !string.IsNullOrWhiteSpace(CurrentPhoneNumber);
    private string? CurrentEmail => RequestContext.User?.GetUserEmail();
    private string? CurrentPhoneNumber => RequestContext.User?.GetUserPhoneNumber();

    protected override void OnParametersSet()
    {
        DateTimeOffset initialRemindAt = Reminder?.RemindAt.ToLocalTime()
            ?? DefaultReminderTime();

        kind = SelectInitialKind(Reminder?.Kind);
        remindDate = initialRemindAt.Date;
        remindTime = initialRemindAt.TimeOfDay;
    }

    private async Task SaveAsync()
    {
        errorMessage = Validate();
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return;
        }

        isSaving = true;
        try
        {
            DateTimeOffset remindAt = ComposeRemindAt();
            EntityResult<ReminderReadDto> result = Reminder is null
                ? await AppointmentReminderService.AddUserReservationReminderAsync(
                    Reservation.Id,
                    kind,
                    remindAt,
                    RequestContext.UserId,
                    GetRecipient(kind),
                    CancellationToken.None)
                : await AppointmentReminderService.UpdateUserReservationReminderAsync(
                    Reservation.Id,
                    Reminder.Id,
                    kind,
                    remindAt,
                    RequestContext.UserId,
                    GetRecipient(kind),
                    CancellationToken.None);

            if (result.IsFailure)
            {
                errorMessage = FormatErrors(result.Errors);
                return;
            }

            MudDialog.Close(DialogResult.Ok(result.Value));
        }
        finally
        {
            isSaving = false;
        }
    }

    private string? Validate()
    {
        if (!HasEmail && !HasSms)
        {
            return Localizer["ReminderContactRequired"].Value;
        }

        if (kind == ReminderKind.Email && !HasEmail)
        {
            return Localizer["EmailRequiredForReminder"].Value;
        }

        if (kind == ReminderKind.Sms && !HasSms)
        {
            return Localizer["PhoneRequiredForReminder"].Value;
        }

        if (remindDate is null || remindTime is null)
        {
            return Localizer["ReminderDateTimeRequired"].Value;
        }

        DateTimeOffset remindAt = ComposeRemindAt();
        if (remindAt <= DateTimeOffset.Now)
        {
            return Localizer["ReminderMustBeFuture"].Value;
        }

        if (Reservation.Occurrence is null)
        {
            return Localizer["ReservationOccurrenceRequired"].Value;
        }

        if (remindAt >= Reservation.Occurrence.OccurrenceStart)
        {
            return Localizer["ReminderMustBeBeforeReservationStart"].Value;
        }

        return null;
    }

    private ReminderKind SelectInitialKind(ReminderKind? existingKind)
    {
        if (existingKind == ReminderKind.Sms && HasSms)
        {
            return ReminderKind.Sms;
        }

        if (existingKind == ReminderKind.Email && HasEmail)
        {
            return ReminderKind.Email;
        }

        return HasSms ? ReminderKind.Sms : ReminderKind.Email;
    }

    private DateTimeOffset DefaultReminderTime()
    {
        DateTimeOffset fallback = DateTimeOffset.Now.AddHours(1);
        if (Reservation.Occurrence is null)
        {
            return fallback;
        }

        DateTimeOffset beforeStart = Reservation.Occurrence.OccurrenceStart.ToLocalTime().AddHours(-1);
        return beforeStart > DateTimeOffset.Now ? beforeStart : fallback;
    }

    private DateTimeOffset ComposeRemindAt()
    {
        DateTime localDateTime = DateTime.SpecifyKind(remindDate!.Value.Date + remindTime!.Value, DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
    }

    private string? GetRecipient(ReminderKind reminderKind) => reminderKind switch
    {
        ReminderKind.Email => CurrentEmail,
        ReminderKind.Sms => CurrentPhoneNumber,
        _ => null
    };

    private static string FormatErrors(IEnumerable<EntityError> errors)
        => string.Join(Environment.NewLine, errors.Select(static error => error.Message));

    private async Task DeleteAsync()
    {
        if (Reminder is null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["DeleteItem", Localizer["Reminder"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["DeleteItem", Localizer["Reminder"]],
            parameters);
        DialogResult? confirmation = await dialog.Result;
        if (confirmation is null || confirmation.Canceled)
        {
            return;
        }

        isSaving = true;
        try
        {
            EntityResult result = await AppointmentReminderService.DeleteUserReservationReminderAsync(
                Reservation.Id,
                Reminder.Id,
                CancellationToken.None);
            if (result.IsFailure)
            {
                errorMessage = FormatErrors(result.Errors);
                return;
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
        finally
        {
            isSaving = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
