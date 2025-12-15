namespace Vitarish.Clients.Interface.Sea.Components.Projects.Plugins;
/// <summary>
/// Carries runtime information about a project row when invoking
/// <see cref="IProjectGridCellDisplayPlugin"/> instances.
/// </summary>
/// <param name="Services">The scoped service provider resolved for the grid.</param>
/// <param name="Project">The project represented by the current row.</param>
/// <param name="Stats">The statistics associated with the project, if available.</param>
/// <param name="OnClicked">Callback triggered when the project row is activated.</param>
/// <param name="HighlightString">The optional term to highlight within the display.</param>
/// <param name="ChildContent">Optional render fragment supplied by the host grid.</param>
/// <param name="Metadata">Additional metadata exposed to plugins.</param>
public sealed record ProjectGridCellDisplayContext(
    IServiceProvider Services,
    ProjectDto Project,
    StatsDto? Stats,
    EventCallback<Guid> OnClicked,
    string? HighlightString,
    RenderFragment<ProjectDto>? ChildContent,
    IReadOnlyDictionary<string, object?> Metadata) : PluginContext(Services, Metadata)
{
    /// <summary>
    /// Initializes a new instance with an empty metadata collection.
    /// </summary>
    /// <param name="services">The scoped service provider resolved for the grid.</param>
    /// <param name="project">The project represented by the current row.</param>
    /// <param name="stats">The statistics associated with the project.</param>
    /// <param name="onClicked">Callback triggered when the project row is activated.</param>
    /// <param name="highlightString">The optional term to highlight within the display.</param>
    /// <param name="childContent">Optional render fragment supplied by the host grid.</param>
    public ProjectGridCellDisplayContext(
        IServiceProvider services,
        ProjectDto project,
        StatsDto? stats,
        EventCallback<Guid> onClicked,
        string? highlightString,
        RenderFragment<ProjectDto>? childContent)
        : this(services, project, stats, onClicked, highlightString, childContent, new Dictionary<string, object?>())
    {
    }
}
