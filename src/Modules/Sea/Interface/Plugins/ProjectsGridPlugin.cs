namespace Vitarish.Sea.Interface.Plugins;

public sealed record ProjectsGridPlugin(
    EventCallback<Guid> OnClicked,
    EventCallback OnAdd,
    bool TemplateMode = false) : PluginContext();
public interface IProjectsGridPlugin : IPlugin<ProjectsGridPlugin>;
