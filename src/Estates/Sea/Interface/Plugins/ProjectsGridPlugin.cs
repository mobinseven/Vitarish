namespace Vitarish.Sea.Interface.Plugins;

internal sealed record ProjectsGridPlugin(
    EventCallback<Guid> OnClicked,
    EventCallback OnAdd,
    bool TemplateMode = false) : PluginContext();
internal interface IProjectsGridPlugin : IPlugin<ProjectsGridPlugin>;
