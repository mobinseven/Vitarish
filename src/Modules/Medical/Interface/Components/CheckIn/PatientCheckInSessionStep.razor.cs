using Google.Protobuf.Reflection;

namespace Vitarish.Medical.Interface.Components.CheckIn;

public partial class PatientCheckInSessionStep : Vitarish.Modules.Base.TenantCommonBase
{
    [Parameter, EditorRequired] public PatientCheckInContext Context { get; set; } = default!;
    [Parameter] public EventCallback OnBooked { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }

    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private ISessionBookableDateQueryService BookableDateQueryService { get; set; } = default!;

    private SessionDescriptorDto sessionDescriptor = default!;
    private SessionDateRangeQuery sessionRange = default!;
    private IReadOnlyList<ReservationReadDto> patientReservations = [];
    private IReadOnlyList<BookableSessionSlot> slots = [];
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
        loadingReservations = true;
        await InvokeAsync(StateHasChanged);

        IReadOnlyList<BookableSessionSlot> result = await BookableDateQueryService
            .GetBookableSlotsAsync(sessionDescriptor, sessionRange, CancellationToken.None);
        if (result.Count == 0)
        {
            slots = [.. result
            .OrderBy(s => s.Occurrence?.Date ?? DateOnly.MaxValue)
            .ThenBy(s => s.Occurrence?.StartTime ?? TimeOnly.MaxValue)
            .ThenBy(s => s.Session.Title)];
        }
        slots = [.. result];

        ReservationDescriptorDto reservationDescriptor = new()
        {
            ReservingPartyId = GetReservingPartyId(),
            DateFrom = today,
            DateTo = today,
            Skip = 0,
            Take = int.MaxValue
        };

        patientReservations = await ReservationManager
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

        EntityResult<ReservationReadDto> reserveResult = await ReservationManager
            .ReserveAsync(session.Id, occurrence.Date, GetReservingPartyId(), CancellationToken.None);

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

    /// <summary>
    /// Returns the reserving-party identifier for the selected patient.
    /// Prefers the linked user account; falls back to the patient's own project ID as a
    /// stable opaque identifier the Appointments module accepts.
    /// </summary>
    private string GetReservingPartyId()
    {
        string? userId = Context.SelectedPatient?.UserIds.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(userId)
            ? userId
            : Context.SelectedPatient!.Id.ToString();
    }
}
