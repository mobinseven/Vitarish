namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectEditPlugin(
    ProjectDto? Project,
    EventCallback<Guid> OnUpdate,
    EventCallback<Guid> OnCreate,
    EventCallback OnCancel,
    bool TemplateMode = false) : PluginContext();
internal interface IProjectEditPlugin : Plugin<ProjectEditPlugin>;
