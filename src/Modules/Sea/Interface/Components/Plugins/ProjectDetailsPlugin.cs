namespace Vitarish.Sea.Interface.Components.Plugins;

internal sealed record ProjectDetailsPlugin(ProjectDto Project, EventCallback OnEdit = default, EventCallback OnDelete = default, EventCallback<Guid> OnProcedureAdded = default) : PluginContext();
internal interface IProjectDetailsPlugin : Plugin<ProjectDetailsPlugin>;
