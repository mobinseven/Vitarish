namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectsGridPlugin(
    EventCallback<Guid> OnClicked,
    EventCallback<Guid> OnCreated) : PluginContext();
internal interface IProjectsGridPlugin : Plugin<ProjectsGridPlugin>;
