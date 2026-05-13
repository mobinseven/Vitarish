namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class BookableSessionsCalendar : Vitarish.Modules.Base.TenantCommonBase
{
    [Parameter] public SessionDescriptorDto? Descriptor { get; set; }
    [Parameter] public SessionDateRangeQuery? Range { get; set; }
    [Parameter] public RenderFragment<BookableSessionSlot>? SlotActions { get; set; }

    [Inject] private ISessionBookableDateQueryService BookableDateQueryService { get; set; } = default!;

    private List<BookableSessionSlot> slots = [];
    private DateOnly today;
    private bool loading;

    private SessionDescriptorDto? previousDescriptor;
    private SessionDateRangeQuery? previousRange;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        today = DateOnly.FromDateTime(DateTime.UtcNow);
        RenderForType<SessionReadDto>();
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

        IReadOnlyList<BookableSessionSlot> result = await BookableDateQueryService
            .GetBookableSlotsAsync(Descriptor, Range, CancellationToken.None);
        if (result.Count == 0)
        {
            slots = [.. result
            .OrderBy(s => s.Occurrence?.Date ?? DateOnly.MaxValue)
            .ThenBy(s => s.Occurrence?.StartTime ?? TimeOnly.MaxValue)
            .ThenBy(s => s.Session.Title)];
        }
        slots = [.. result];
        loading = false;
        await InvokeAsync(StateHasChanged);
    }
}
