using System.Reflection;

namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventItemsTimeline : EventsBase
{
    private static readonly MethodInfo? RenderForTypeMethod = ResolveRenderForTypeMethod();

    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;

    [Inject] private IEnumerable<IEventItemPlugin> Plugins { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private MudDataGrid<EventTimelineItem>? grid;
    private readonly List<EventTimelineItem> items = [];
    private readonly List<(IEventItemPlugin Plugin, EventTimelineContext Context)> pluginContexts = [];

    private bool CanWrite { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = LoadRolesAsync();
        UpdatePluginContexts();
    }

    private async Task<GridData<EventTimelineItem>> ReloadData(GridState<EventTimelineItem> gridState)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        List<EventTimelineItem> aggregated = [];
        foreach ((IEventItemPlugin plugin, EventTimelineContext context) in pluginContexts)
        {
            IReadOnlyList<EventTimelineItem> pluginItems = await plugin.GetItemsAsync(context, cancellationToken);
            aggregated.AddRange(pluginItems);
        }

        items.Clear();
        items.AddRange(aggregated.OrderBy(item => item.When));

        return new GridData<EventTimelineItem>
        {
            Items = items,
            TotalItems = items.Count
        };
    }

    protected override async Task ReloadRequired()
    {
        if (grid is not null)
        {
            await grid.ReloadServerData();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadRolesAsync()
    {
        CanWrite = AdminMode || await AccessService.CanWriteEvent(Event.Id);
        await InvokeAsync(StateHasChanged);
    }

    private static MethodInfo? ResolveRenderForTypeMethod()
    {
        Type? current = typeof(SeaCommonBase);
        while (current is not null)
        {
            MethodInfo? method = current.GetMethod("RenderForType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method is not null && method.IsGenericMethodDefinition)
            {
                return method;
            }

            current = current.BaseType;
        }

        return null;
    }

    private void UpdatePluginContexts()
    {
        Dictionary<string, object?> metadata = new()
        {
            ["ProjectId"] = Project.Id,
            ["ProcedureId"] = Procedure.Id,
            ["EventId"] = Event.Id
        };

        pluginContexts.Clear();

        foreach (IEventItemPlugin plugin in Plugins)
        {
            EventTimelineContext context = new(ServiceProvider, Project, Procedure, Event, Color, metadata);
            if (plugin.IsAvailable(context))
            {
                pluginContexts.Add((plugin, context));
            }
        }

        SubscribeToWatchedTypes();

        if (grid is not null)
        {
            _ = grid.ReloadServerData();
        }
    }

    private void SubscribeToWatchedTypes()
    {
        if (RenderForTypeMethod is null)
        {
            return;
        }

        HashSet<Type> watchedTypes = [typeof(EventDto)];
        foreach ((IEventItemPlugin plugin, _) in pluginContexts)
        {
            watchedTypes.UnionWith(plugin.WatchedTypes);
        }

        foreach (Type type in watchedTypes)
        {
            MethodInfo method = RenderForTypeMethod.MakeGenericMethod(type);
            _ = method.Invoke(this, null);
        }
    }
}
