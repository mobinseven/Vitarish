namespace Vitarish.Sea.Interface.Components.Events.Plugins;

public record EventItemPlugin(
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event) : PluginContext();

public sealed record EventItemCreationContext(
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event) : EventItemPlugin(Project, Procedure, Event)
{
    public RenderFragment? Actions { get; init; }
}

public sealed record EventTimelineContext(
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event,
    Color Color) : EventItemPlugin(Project, Procedure, Event);

public sealed record EventTimelineItem(
    DateTime When,
    RenderFragment Content,
    IReadOnlyList<Guid> Readers,
    string? Placeholder = null);

public enum EventItemCreationPlacement
{
    Main,
    Action
}
public interface IEventItemCreationPlugin : IPlugin<EventItemCreationContext>
{
    EventItemCreationPlacement Placement { get; }
}

public interface IEventItemTimelinePlugin
{
    string Id { get; }

    bool IsAvailable(EventTimelineContext context);

    Task<IReadOnlyList<EventTimelineItem>> GetItemsAsync(EventTimelineContext context, CancellationToken cancellationToken);
}

public interface IEventItemPlugin : IEventItemCreationPlugin, IEventItemTimelinePlugin;
