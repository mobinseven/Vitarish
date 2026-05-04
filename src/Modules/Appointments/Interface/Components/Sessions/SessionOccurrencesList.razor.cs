namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionOccurrencesList : SessionsBase
{
    [Parameter] public Guid? SessionId { get; set; }
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }

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

        // Occurrences are resolved via a dedicated service; loaded empty until a resolver is wired up.
        occurrences = [];
        await InvokeAsync(StateHasChanged);
    }
}
