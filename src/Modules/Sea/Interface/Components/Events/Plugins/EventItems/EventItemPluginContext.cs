namespace Vitarish.Sea.Interface.Components.Events.Plugins.EventItems;

public record EventItemPluginContext(
    IServiceProvider Services,
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event,
    IReadOnlyDictionary<string, object?> Metadata) : PluginContext(Services, Metadata);

public sealed record EventItemCreationContext(
    IServiceProvider Services,
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event,
    IReadOnlyDictionary<string, object?> Metadata) : EventItemPluginContext(Services, Project, Procedure, Event, Metadata)
{
    public RenderFragment? Actions { get; init; }
}

public sealed record EventTimelineContext(
    IServiceProvider Services,
    ProjectDto Project,
    ProcedureDto Procedure,
    EventDto Event,
    Color Color,
    IReadOnlyDictionary<string, object?> Metadata) : EventItemPluginContext(Services, Project, Procedure, Event, Metadata);

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
