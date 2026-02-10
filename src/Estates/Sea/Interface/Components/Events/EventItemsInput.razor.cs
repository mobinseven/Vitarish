namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventItemsInput : EventsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter] public EventCallback OnCreated { get; set; }
    [Inject] private IEnumerable<IEventItemPlugin> Plugins { get; set; } = default!;

    private readonly List<IEventItemCreationPlugin> mainPlugins = [];
    private readonly List<IEventItemCreationPlugin> actionPlugins = [];

    private EventItemCreationContext baseContext = default!;
    private EventItemCreationContext contextWithActions = default!;
    private RenderFragment? actionsFragment;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        baseContext = new EventItemCreationContext(Project, Procedure, Event, OnCreated);
        mainPlugins.Clear();
        actionPlugins.Clear();

        foreach (IEventItemPlugin plugin in Plugins)
        {
            switch (plugin.Placement)
            {
                case EventItemCreationPlacement.Main:
                    mainPlugins.Add(plugin);
                    break;
                case EventItemCreationPlacement.Action:
                    actionPlugins.Add(plugin);
                    break;
            }
        }

        actionsFragment = actionPlugins.Count == 0
            ? null
            : BuildActionsFragment(baseContext);

        contextWithActions = baseContext with { Actions = actionsFragment };
    }

    private RenderFragment BuildActionsFragment(EventItemCreationContext context)
        => builder =>
        {
            foreach (IEventItemCreationPlugin plugin in actionPlugins)
            {
                builder.AddContent(0, plugin.Render(context));
            }
        };
}
