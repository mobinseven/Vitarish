namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventItemsTimeline : EventsBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;

    [Inject] private IEnumerable<IEventItemPlugin> Plugins { get; set; } = default!;

    private bool CanWrite;

    private readonly List<EventTimelineItem> items = [];
    private readonly List<(IEventItemPlugin Plugin, EventTimelineContext Context)> pluginContexts = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await UpdatePluginContexts();
        CanWrite = await EventsService.CanWriteEvent(Event.Id);
    }

    protected override async Task ReloadRequired()
    {
        List<EventTimelineItem> aggregated = [];
        foreach ((IEventItemPlugin plugin, EventTimelineContext context) in pluginContexts)
        {
            IReadOnlyList<EventTimelineItem> pluginItems = await plugin.GetItemsAsync(context, CancellationToken.None);
            aggregated.AddRange(pluginItems);
        }

        items.Clear();
        items.AddRange(aggregated.OrderBy(item => item.When));

        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdatePluginContexts()
    {
        pluginContexts.Clear();

        EventCallback OnDelete = EventCallback.Factory.Create(this, () => ReloadRequired());

        foreach (IEventItemPlugin plugin in Plugins)
        {
            EventTimelineContext context = new(Project, Procedure, Event, Color, OnDelete);
            if (plugin.IsAvailable(context))
            {
                pluginContexts.Add((plugin, context));
            }
        }

        await ReloadRequired();
    }
}
