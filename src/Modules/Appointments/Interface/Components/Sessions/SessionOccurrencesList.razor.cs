namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionOccurrencesList : SessionsBase
{
    [Parameter] public Guid? SessionId { get; set; }
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }

    [Inject] private ISessionOccurrenceResolver SessionOccurrenceResolver { get; set; } = default!;

    private List<SessionOccurrenceReadDto> occurrences = [];

    protected override async Task OnParametersSetAsync()
    {
        await ReloadRequired();
    }

    protected override async Task ReloadRequired()
    {
        if (SessionId is null)
        {
            occurrences = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        TimeRangeQuery range = new()
        {
            Start = DateTimeOffset.UtcNow.AddDays(-1),
            End = DateTimeOffset.UtcNow.AddYears(1)
        };

        IReadOnlyList<SessionOccurrenceReadDto> resolved = await SessionOccurrenceResolver.ResolveOccurrencesAsync(SessionId.Value, range, CancellationToken.None);
        occurrences = [.. resolved.OrderBy(o => o.OccurrenceStart)];
        await InvokeAsync(StateHasChanged);
    }
}
