namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectsGridPlugin(
    EventCallback<Guid> OnClicked,
    EventCallback OnAdd,
    bool TemplateMode = false) : PluginContext();
internal interface IProjectsGridPlugin : Plugin<ProjectsGridPlugin>;
