namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectDetailsPlugin(
    ProjectDto Project,
    EventCallback<Guid> OnEdit = default,
    EventCallback OnDeleted = default) : PluginContext();
internal interface IProjectDetailsPlugin : Plugin<ProjectDetailsPlugin>;
