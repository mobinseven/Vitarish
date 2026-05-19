using Google.Protobuf.Reflection;

namespace Vitarish.Clinic.Interface.Components.CheckIn;

public partial class PatientCheckInShiftStep : Modules.Base.TenantCommonBase
{
    [Parameter, EditorRequired] public PatientCheckInContext Context { get; set; } = default!;
    [Parameter] public EventCallback OnBooked { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }

    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private IShiftBookableDateQueryService BookableDateQueryService { get; set; } = default!;

    private ShiftDescriptorDto shiftDescriptor = default!;
    private ShiftDateRangeQuery shiftRange = default!;
    private IReadOnlyList<ReservationReadDto> patientReservations = [];
    private IReadOnlyList<BookableShiftSlot> slots = [];
    private DateOnly today;
    private bool loadingReservations;
    private bool booking;
    private string? bookingError;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        today = DateOnly.FromDateTime(DateTime.UtcNow);
        shiftDescriptor = new ShiftDescriptorDto
        {
            Status = ShiftStatus.Active,
            Skip = 0,
            Take = int.MaxValue
        };
        shiftRange = new ShiftDateRangeQuery
        {
            DateFrom = today,
            DateTo = today.AddDays(30)
        };
    }

    protected override async Task ReloadRequired()
    {
        loadingReservations = true;
        await InvokeAsync(StateHasChanged);

        IReadOnlyList<BookableShiftSlot> result = await BookableDateQueryService
            .GetBookableSlotsAsync(shiftDescriptor, shiftRange, CancellationToken.None);
        slots = [.. result
            .OrderBy(s => s.Occurrence?.Date ?? DateOnly.MaxValue)
            .ThenBy(s => s.Occurrence?.StartTime ?? TimeOnly.MaxValue)
            .ThenBy(s => s.Shift.DayOfWeek)
            .ThenBy(s => s.Shift.EndTime)];

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

    private async Task BookSlotAsync(ShiftReadDto shift, ShiftReservationReadDto occurrence)
    {
        bookingError = null;
        booking = true;
        await InvokeAsync(StateHasChanged);

        EntityResult<ReservationReadDto> reserveResult = await ReservationManager
            .ReserveAsync(shift.Id, occurrence.Date, GetReservingPartyId(), CancellationToken.None);

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

        Context.SelectedShift = shift;
        Context.SelectedOccurrence = occurrence;
        Context.ConfirmedReservation = reservation;
        Context.Outcome = occurrence.Date == today ? CheckInOutcome.CurrentShift : CheckInOutcome.FutureShift;

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
