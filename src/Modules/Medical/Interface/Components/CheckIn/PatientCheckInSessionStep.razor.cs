namespace Vitarish.Medical.Interface.Components.CheckIn;

public partial class PatientCheckInSessionStep : Vitarish.Modules.Base.TenantCommonBase
{
    [Parameter, EditorRequired] public PatientCheckInContext Context { get; set; } = default!;
    [Parameter] public EventCallback OnBooked { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }

    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;

    private SessionDescriptorDto sessionDescriptor = default!;
    private SessionDateRangeQuery sessionRange = default!;
    private IReadOnlyList<ReservationReadDto> patientTodayReservations = [];
    private DateOnly today;
    private bool loadingReservations;
    private bool booking;
    private string? bookingError;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        today = DateOnly.FromDateTime(DateTime.UtcNow);
        sessionDescriptor = new SessionDescriptorDto
        {
            Status = SessionStatus.Active,
            Skip = 0,
            Take = int.MaxValue
        };
        sessionRange = new SessionDateRangeQuery
        {
            DateFrom = today,
            DateTo = today.AddDays(30)
        };
    }

    protected override async Task ReloadRequired()
    {
        await LoadPatientReservationsAsync();
    }

    private async Task LoadPatientReservationsAsync()
    {
        loadingReservations = true;
        await InvokeAsync(StateHasChanged);

        // Prefer the linked user account; fall back to the patient's own project ID as a
        // stable party identifier — the Appointments module treats it as an opaque string.
        string? patientUserId = Context.SelectedPatient?.UserIds.FirstOrDefault();
        string reservingPartyId = !string.IsNullOrWhiteSpace(patientUserId)
            ? patientUserId
            : Context.SelectedPatient!.Id.ToString();

        ReservationDescriptorDto reservationDescriptor = new()
        {
            ReservingPartyId = reservingPartyId,
            Date = today,
            Skip = 0,
            Take = int.MaxValue
        };

        patientTodayReservations = await ReservationManager
            .GetListAsync(reservationDescriptor, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];

        loadingReservations = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task BookSlotAsync(SessionReadDto session, SessionReservationReadDto occurrence)
    {
        bookingError = null;
        booking = true;
        await InvokeAsync(StateHasChanged);

        // Prefer the linked user account; fall back to the patient's own project ID as a
        // stable party identifier — the Appointments module treats it as an opaque string.
        string? patientUserId = Context.SelectedPatient?.UserIds.FirstOrDefault();
        string reservingPartyId = !string.IsNullOrWhiteSpace(patientUserId)
            ? patientUserId
            : Context.SelectedPatient!.Id.ToString();

        EntityResult<ReservationReadDto> reserveResult = await ReservationManager
            .ReserveAsync(session.Id, occurrence.Date, reservingPartyId, CancellationToken.None);

        if (reserveResult.IsFailure || reserveResult.Value is null)
        {
            bookingError = reserveResult.Errors.FirstOrDefault()?.Message ?? Localizer["Error"].Value;
            booking = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        ReservationReadDto reservation = reserveResult.Value;

        if (Context.MarkAsArrived && occurrence.Date == today)
        {
            EntityResult confirmResult = await ReservationManager
                .ConfirmReservationAsync(reservation.Id, CancellationToken.None);

            if (!confirmResult.IsFailure)
            {
                _ = await ReservationManager
                    .PresentReservationAsync(reservation.Id, CancellationToken.None);
            }
        }

        Context.SelectedSession = session;
        Context.SelectedOccurrence = occurrence;
        Context.ConfirmedReservation = reservation;
        Context.Outcome = occurrence.Date == today ? CheckInOutcome.CurrentSession : CheckInOutcome.FutureSession;

        booking = false;
        await OnBooked.InvokeAsync();
    }
}
