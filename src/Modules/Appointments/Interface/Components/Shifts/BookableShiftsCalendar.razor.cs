namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class BookableShiftsCalendar : Vitarish.Modules.Base.TenantCommonBase
{
    [Parameter] public ShiftDescriptorDto? Descriptor { get; set; }
    [Parameter] public ShiftDateRangeQuery? Range { get; set; }
    [Parameter] public RenderFragment<BookableShiftSlot>? SlotActions { get; set; }
    [Parameter] public EventCallback<BookableShiftSlot> OnSelected { get; set; }

    [Inject] private IShiftBookableDateQueryService BookableDateQueryService { get; set; } = default!;

    private List<BookableShiftSlot> slots = [];
    private DateOnly today;
    private bool loading;

    private ShiftDescriptorDto? previousDescriptor;
    private ShiftDateRangeQuery? previousRange;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        today = DateOnly.FromDateTime(DateTime.UtcNow);
        RenderForType<ShiftReadDto>();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Descriptor != previousDescriptor || Range != previousRange)
        {
            previousDescriptor = Descriptor;
            previousRange = Range;
            await ReloadRequired();
        }
    }

    protected override async Task ReloadRequired()
    {
        if (Descriptor is null || Range is null)
        {
            slots = [];
            await InvokeAsync(StateHasChanged);
            return;
        }

        loading = true;
        await InvokeAsync(StateHasChanged);

        IReadOnlyList<BookableShiftSlot> result = await BookableDateQueryService
            .GetBookableSlotsAsync(Descriptor, Range, CancellationToken.None);
        slots = [.. result
            .OrderBy(s => s.Occurrence?.Date ?? DateOnly.MaxValue)
            .ThenBy(s => s.Occurrence?.StartTime ?? TimeOnly.MaxValue)
            .ThenBy(s => s.Shift.DayOfWeek)
            .ThenBy(s => s.Shift.EndTime)];
        loading = false;
        await InvokeAsync(StateHasChanged);
    }
}
