namespace Vitarish.Sea.Interface.Plugins;

internal sealed record ProjectEditPlugin(
    ProjectReadDto? Project,
    EventCallback<Guid> OnUpdate,
    EventCallback<Guid> OnCreate,
    EventCallback OnCancel,
    bool TemplateMode = false) : PluginContext();
internal interface IProjectEditPlugin : IPlugin<ProjectEditPlugin>;
