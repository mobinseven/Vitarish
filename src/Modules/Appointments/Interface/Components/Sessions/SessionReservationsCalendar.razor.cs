namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReservationsCalendar
{
    [Parameter] public Guid? SessionId { get; set; }
    [Parameter] public EventCallback<SessionReservationReadDto> OnSelected { get; set; }
    [Inject] private ISessionOccurrenceQueryService OccurrenceQueryService { get; set; } = default!;

    private List<SessionReservationReadDto> occurrences = [];

    protected override async Task ReloadRequired()
    {
        if (SessionId is null)
        {
            occurrences = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        SessionDateRangeQuery range = new()
        {
            DateFrom = today,
            DateTo = today.AddYears(1)
        };

        IReadOnlyList<SessionReservationReadDto> resolved = await OccurrenceQueryService.GetBookableDatesAsync(SessionId.Value, range, CancellationToken.None);
        occurrences = [.. resolved.OrderBy(o => o.Date).ThenBy(o => o.StartTime)];
        await InvokeAsync(StateHasChanged);
    }
}
