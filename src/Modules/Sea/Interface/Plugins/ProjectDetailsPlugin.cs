namespace Vitarish.Sea.Interface.Plugins;

public sealed record ProjectDetailsPlugin(
    ProjectReadDto Project,
    EventCallback<Guid> OnEdit = default,
    EventCallback OnDeleted = default) : PluginContext();
public interface IProjectDetailsPlugin : IPlugin<ProjectDetailsPlugin>;
