namespace Vitarish.Clients.Interface.Sea.Components.Projects.Plugins;
/// <summary>
/// Describes a plugin that can render the primary project cell inside the
/// projects grid. Implementations can replace the default presentation with
/// tailored visuals or interactions while reusing the host grid.
/// </summary>
public interface IProjectGridCellDisplayPlugin : IPlugin<ProjectGridCellDisplayContext>
{ }
