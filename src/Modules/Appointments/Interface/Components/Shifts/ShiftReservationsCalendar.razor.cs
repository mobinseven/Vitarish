namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftReservationsCalendar
{
    [Parameter] public Guid? ShiftId { get; set; }
    [Parameter] public EventCallback<ShiftReservationReadDto> OnSelected { get; set; }
    [Inject] private IShiftBookableDateQueryService BookableDateQueryService { get; set; } = default!;

    private List<ShiftReservationReadDto> occurrences = [];

    protected override async Task ReloadRequired()
    {
        if (ShiftId is null)
        {
            occurrences = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        ShiftDateRangeQuery range = new()
        {
            DateFrom = today,
            DateTo = today.AddYears(1)
        };

        IReadOnlyList<ShiftReservationReadDto> resolved = await BookableDateQueryService.GetBookableDatesAsync(ShiftId.Value, range, CancellationToken.None);
        occurrences = [.. resolved.OrderBy(o => o.Date).ThenBy(o => o.StartTime)];
        await InvokeAsync(StateHasChanged);
    }
}
