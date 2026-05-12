using Vitarish.Communications.Interface.Components.Reminders;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class PatientAppointmentBookingDialog : Vitarish.Modules.Base.TenantCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectReadDto Patient { get; set; } = default!;

    [Inject] private IAccessControlledSessionManager SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private ISessionBookableDateQueryService BookableDateQueryService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string? patientUserId;
    private List<SessionReadDto> sessions = [];
    private bool createReminder;

    protected override async Task OnInitializedAsync()
    {
        patientUserId = Patient.UserIds.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(patientUserId))
        {
            await LoadSessionsAsync();
        }
    }

    private async Task LoadSessionsAsync()
    {
        SessionDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionReadDto>? result = await SessionsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        sessions = [.. (result ?? [])
            .Where(s => s.Status == SessionStatus.Active)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.Title)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(SessionReadDto session)
    {
        if (string.IsNullOrWhiteSpace(patientUserId))
        {
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        SessionDateRangeQuery range = new()
        {
            DateFrom = today,
            DateTo = today.AddYears(1)
        };

        SessionReservationReadDto? sessionReservation = (await BookableDateQueryService.GetBookableDatesAsync(session.Id, range, CancellationToken.None))
            .OrderBy(sr => sr.Date).ThenBy(sr => sr.StartTime)
            .FirstOrDefault(sr => sr.Date >= today && sr.AvailableSeats > 0);

        if (sessionReservation is null)
        {
            _ = Snackbar.Add(Localizer["OccurrenceUnavailable"].Value, Severity.Warning);
            return;
        }

        EntityResult<ReservationReadDto> result = await ReservationManager.ReserveAsync(session.Id, sessionReservation.Date, patientUserId, CancellationToken.None);
        if (result.IsFailure || result.Value is null)
        {
            _ = Snackbar.Add(result.Errors.FirstOrDefault()?.Message ?? Localizer["Error"].Value, Severity.Error);
            return;
        }

        ReservationReadDto reservation = result.Value;
        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);

        if (createReminder)
        {
            DialogParameters parameters = new()
            {
                { nameof(ReminderCreatorDialog.SourceId), reservation.Id }
            };
            IDialogReference reminderDialog = await DialogService.ShowAsync<ReminderCreatorDialog>(null, parameters);
            _ = await reminderDialog.Result;
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
