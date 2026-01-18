namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectEditPlugin(
    ProjectDto Project,
    Action<ProjectDto> OnUpdate) : PluginContext();
internal interface IProjectEditPlugin : Plugin<ProjectEditPlugin>;
