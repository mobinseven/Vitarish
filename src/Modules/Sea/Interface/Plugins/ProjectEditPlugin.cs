namespace Vitarish.Sea.Interface.Plugins;

public sealed record ProjectEditPlugin(
    ProjectReadDto? Project,
    EventCallback<Guid> OnUpdate,
    EventCallback<Guid> OnCreate,
    EventCallback OnCancel,
    bool TemplateMode = false) : PluginContext();
public interface IProjectEditPlugin : IPlugin<ProjectEditPlugin>;
