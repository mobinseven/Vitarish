namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionsList : SessionsBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback<DayOfWeek?> OnAdd { get; set; }
    [Parameter] public SessionDescriptorDto? Descriptor { get; set; } = new();

    [Inject] private IEnumerable<ISessionsGridPlugin> GridPlugins { get; set; } = [];
    private ISessionsGridPlugin? Plugin => GridPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private SessionDescriptorDto? previousDescriptor;
    protected List<SessionReadDto> sessions = [];

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task OnParametersSetAsync()
    {
        if (Descriptor != previousDescriptor)
        {
            previousDescriptor = Descriptor;
            await ReloadRequired();
        }
    }

    protected override async Task ReloadRequired()
    {
        SessionDescriptorDto descriptor = Descriptor is not null
            ? Descriptor with { Skip = 0, Take = int.MaxValue, Search = SearchString }
            : new() { Skip = 0, Take = int.MaxValue, Search = SearchString };

        IReadOnlyList<SessionReadDto> items = await SessionManager.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        sessions = [.. items
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.EndTime)];

        await InvokeAsync(StateHasChanged);
    }
}
