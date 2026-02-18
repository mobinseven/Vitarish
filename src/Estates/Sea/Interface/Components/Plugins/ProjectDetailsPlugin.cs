namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectDetailsPlugin(
    ProjectReadDto Project,
    EventCallback<Guid> OnEdit = default,
    EventCallback OnDeleted = default) : PluginContext();
internal interface IProjectDetailsPlugin : IPlugin<ProjectDetailsPlugin>;
