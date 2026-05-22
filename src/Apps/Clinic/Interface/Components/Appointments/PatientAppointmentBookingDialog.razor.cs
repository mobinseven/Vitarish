using Vitarish.Communications.Interface.Components.Reminders;

namespace Vitarish.Clinic.Interface.Components.Appointments;

public partial class PatientAppointmentBookingDialog : Modules.Base.TenantCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectReadDto Patient { get; set; } = default!;

    [Inject] private IAccessControlledShiftManager ShiftsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private IShiftBookableDateQueryService BookableDateQueryService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string? patientUserId;
    private List<ShiftReadDto> shifts = [];
    private bool createReminder;
    private TimeSpan? estimatedSessionLengthOverride;

    protected override async Task OnInitializedAsync()
    {
        patientUserId = Patient.UserIds.FirstOrDefault();
        await LoadShiftsAsync();
    }

    private async Task LoadShiftsAsync()
    {
        ShiftDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<ShiftReadDto>? result = await ShiftsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        shifts = [.. (result ?? [])
            .Where(s => s.Status == ShiftStatus.Active)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.EndTime)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(ShiftReadDto shift)
    {
        // Prefer the linked user account; fall back to the patient's own project ID as a
        // stable party identifier — the Appointments module treats it as an opaque string.
        string reservingPartyId = !string.IsNullOrWhiteSpace(patientUserId)
            ? patientUserId
            : Patient.Id.ToString();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        ShiftDateRangeQuery range = new()
        {
            DateFrom = today,
            DateTo = today.AddYears(1)
        };

        ShiftReservationReadDto? shiftReservation = (await BookableDateQueryService.GetBookableDatesAsync(shift.Id, range, CancellationToken.None))
            .OrderBy(sr => sr.Date).ThenBy(sr => sr.StartTime)
            .FirstOrDefault(sr => sr.Date >= today && sr.AvailableSeats > 0);

        if (shiftReservation is null)
        {
            _ = Snackbar.Add(Localizer["OccurrenceUnavailable"].Value, Severity.Warning);
            return;
        }

        EntityResult<ReservationReadDto> result = await ReservationManager.ReserveAsync(
            shift.Id,
            shiftReservation.Date,
            reservingPartyId,
            estimatedSessionLengthOverride,
            CancellationToken.None);
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
