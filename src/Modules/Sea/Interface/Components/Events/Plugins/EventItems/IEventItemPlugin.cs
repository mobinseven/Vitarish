namespace Vitarish.Sea.Interface.Components.Events.Plugins.EventItems;

public interface IEventItemCreationPlugin : IPlugin<EventItemCreationContext>
{
    EventItemCreationPlacement Placement { get; }
}

public interface IEventItemTimelinePlugin
{
    string Id { get; }

    IReadOnlyCollection<Type> WatchedTypes { get; }

    bool IsAvailable(EventTimelineContext context);

    Task<IReadOnlyList<EventTimelineItem>> GetItemsAsync(EventTimelineContext context, CancellationToken cancellationToken);
}

public interface IEventItemPlugin : IEventItemCreationPlugin, IEventItemTimelinePlugin;
