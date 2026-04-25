namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AppointmentsList : AppointmentsBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public AppointmentDescriptorDto? Descriptor { get; set; }

    [Inject] private IEnumerable<IAppointmentCardPlugin> CardPlugins { get; set; } = [];
    private IAppointmentCardPlugin? Plugin => CardPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private AppointmentDescriptorDto? previousDescriptor;

    protected List<AppointmentReadDto> appointments = [];

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
        AppointmentDescriptorDto descriptor = Descriptor is not null
            ? Descriptor with { Skip = 0, Take = int.MaxValue, Search = SearchString }
            : new() { Skip = 0, Take = int.MaxValue, Search = SearchString };

        IReadOnlyList<AppointmentReadDto> items = await AppointmentsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        appointments = [.. items.OrderBy(a => a.ScheduledAt)];

        await InvokeAsync(StateHasChanged);
    }

    protected virtual Task AddAppointment() => OnAdd.InvokeAsync();
}
