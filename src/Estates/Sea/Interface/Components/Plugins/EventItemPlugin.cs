namespace Vitarish.Sea.Interface.Components.Plugins;

public record EventItemPlugin(
    ProjectReadDto Project,
    ProcedureReadDto Procedure,
    EventReadDto Event) : PluginContext();

public sealed record EventItemCreationContext(
    ProjectReadDto Project,
    ProcedureReadDto Procedure,
    EventReadDto Event,
    EventCallback OnCreated) : EventItemPlugin(Project, Procedure, Event)
{
    public RenderFragment? Actions { get; init; }
}

public sealed record EventTimelineContext(
    ProjectReadDto Project,
    ProcedureReadDto Procedure,
    EventReadDto Event,
    Color Color,
    EventCallback OnDeleted) : EventItemPlugin(Project, Procedure, Event);

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
