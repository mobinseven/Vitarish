namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class AppointmentSessionsList : AppointmentSessionsBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public AppointmentSessionDescriptorDto? Descriptor { get; set; } = new();

    [Inject] private IEnumerable<ISessionsGridPlugin> GridPlugins { get; set; } = [];
    private ISessionsGridPlugin? Plugin => GridPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private AppointmentSessionDescriptorDto? previousDescriptor;

    protected List<AppointmentSessionReadDto> sessions = [];

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
        AppointmentSessionDescriptorDto descriptor = Descriptor is not null
            ? Descriptor with { Skip = 0, Take = int.MaxValue, Search = SearchString }
            : new() { Skip = 0, Take = int.MaxValue, Search = SearchString };

        IReadOnlyList<AppointmentSessionReadDto> items = await SessionsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        sessions = [.. items.OrderBy(s => s.DateTime)];

        await InvokeAsync(StateHasChanged);
    }

    protected virtual Task AddSession() => OnAdd.InvokeAsync();
}
