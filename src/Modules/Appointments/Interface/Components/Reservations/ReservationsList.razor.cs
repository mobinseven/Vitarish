namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationsList : ReservationsBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public ReservationDescriptorDto? Descriptor { get; set; } = new();

    [Inject] private IEnumerable<IAppointmentsGridPlugin> CardPlugins { get; set; } = [];
    private IAppointmentsGridPlugin? Plugin => CardPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private ReservationDescriptorDto? previousDescriptor;
    protected List<ReservationReadDto> reservations = [];

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
        ReservationDescriptorDto descriptor = Descriptor is not null
            ? Descriptor with { Skip = 0, Take = int.MaxValue, Search = SearchString }
            : new() { Skip = 0, Take = int.MaxValue, Search = SearchString };

        IReadOnlyList<ReservationReadDto> items = await ReservationManager.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        reservations = [.. items.OrderBy(r => r.Date)];

        await InvokeAsync(StateHasChanged);
    }
}
