namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftsList : ShiftsBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback<DayOfWeek?> OnAdd { get; set; }
    [Parameter] public ShiftDescriptorDto? Descriptor { get; set; } = new();
    [Parameter] public EventCallback<ShiftReservationReadDto> OnShiftOccurrenceSelected { get; set; }

    [Inject] private IEnumerable<IShiftsGridPlugin> GridPlugins { get; set; } = [];
    private IShiftsGridPlugin? Plugin => GridPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private ShiftDescriptorDto? previousDescriptor;
    protected List<ShiftReadDto> shifts = [];

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
        ShiftDescriptorDto descriptor = Descriptor is not null
            ? Descriptor with { Skip = 0, Take = int.MaxValue, Search = SearchString }
            : new() { Skip = 0, Take = int.MaxValue, Search = SearchString };

        IReadOnlyList<ShiftReadDto> items = await ShiftManager.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        shifts = [.. items
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.EndTime)];

        await InvokeAsync(StateHasChanged);
    }
}
